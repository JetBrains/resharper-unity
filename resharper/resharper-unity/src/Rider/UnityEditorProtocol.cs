using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.changes;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.DocumentModel;
using JetBrains.IDE;
using JetBrains.Lifetimes;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.ProjectModel;
using JetBrains.Rd;
using JetBrains.Rd.Base;
using JetBrains.Rd.Impl;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Rider.Model;
using JetBrains.Rider.Model.Notifications;
using JetBrains.Rider.Unity.Editor.NonUnity;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.dataStructures.TypedIntrinsics;
using JetBrains.Util.Special;
using Newtonsoft.Json;
using RunMethodData = JetBrains.Platform.Unity.EditorPluginModel.RunMethodData;
using UnityApplicationData = JetBrains.Rider.Model.UnityApplicationData;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityEditorProtocol
    {
        private readonly JetHashSet<FileSystemPath> myPluginInstallations;

        private readonly Lifetime myComponentLifetime;
        private readonly SequentialLifetimes mySessionLifetimes;
        private readonly ILogger myLogger;
        private readonly IScheduler myDispatcher;
        private readonly IShellLocks myLocks;
        private readonly ISolution mySolution;
        private readonly JetBrains.Application.ActivityTrackingNew.UsageStatistics myUsageStatistics;
        private readonly IThreading myThreading;
        private readonly UnityVersion myUnityVersion;
        private readonly NotificationsModel myNotificationsModel;
        private readonly IHostProductInfo myHostProductInfo;
        private readonly UnityHost myHost;
        private readonly IContextBoundSettingsStoreLive myBoundSettingsStore;

        [NotNull]
        public readonly ViewableProperty<EditorPluginModel> UnityModel = new ViewableProperty<EditorPluginModel>(null);

        [NotNull]
        public readonly ViewableProperty<SocketWire.Base> UnityWire = new ViewableProperty<SocketWire.Base>(null);

        public UnityEditorProtocol(Lifetime lifetime, ILogger logger, UnityHost host,
                                   IScheduler dispatcher, IShellLocks locks, ISolution solution,
                                   IApplicationWideContextBoundSettingStore settingsStore,
                                   JetBrains.Application.ActivityTrackingNew.UsageStatistics usageStatistics,
                                   UnitySolutionTracker unitySolutionTracker, IThreading threading,
                                   UnityVersion unityVersion, NotificationsModel notificationsModel,
                                   IHostProductInfo hostProductInfo, IFileSystemTracker fileSystemTracker)
        {
            myPluginInstallations = new JetHashSet<FileSystemPath>();

            myComponentLifetime = lifetime;
            myLogger = logger;
            myDispatcher = dispatcher;
            myLocks = locks;
            mySolution = solution;
            myUsageStatistics = usageStatistics;
            myThreading = threading;
            myUnityVersion = unityVersion;
            myNotificationsModel = notificationsModel;
            myHostProductInfo = hostProductInfo;
            myHost = host;
            myBoundSettingsStore = settingsStore.BoundSettingsStore;
            mySessionLifetimes = new SequentialLifetimes(lifetime);

            if (solution.GetData(ProjectModelExtensions.ProtocolSolutionKey) == null)
                return;

            unitySolutionTracker.IsUnityProject.View(lifetime, (lf, args) =>
            {
                if (!args) return;

                var solFolder = mySolution.SolutionDirectory;
                AdviseModelData(lifetime);

                // todo: consider non-Unity Solution with Unity-generated projects
                var protocolInstancePath = solFolder.Combine("Library/ProtocolInstance.json");
                fileSystemTracker.AdviseFileChanges(lf, protocolInstancePath, OnChangeAction);
                // connect on start of Rider
                CreateProtocols(protocolInstancePath);
            });
        }


        private DateTime myLastChangeTime;
        
        private void OnChangeAction(FileSystemChangeDelta delta)
        {
            // connect on reload of server
            if (delta.ChangeType != FileSystemChangeType.ADDED && delta.ChangeType != FileSystemChangeType.CHANGED) return;
            if (delta.NewPath.FileModificationTimeUtc == myLastChangeTime) return;
            myLastChangeTime = delta.NewPath.FileModificationTimeUtc;
            if (!myComponentLifetime.IsTerminated)
                myLocks.ExecuteOrQueue(myComponentLifetime, "CreateProtocol",
                    () => CreateProtocols(delta.NewPath));
        }

        private void AdviseModelData(Lifetime lifetime)
        {
           myHost.PerformModelAction(rd => rd.Play.Advise(lifetime, p => UnityModel.Value.IfNotNull(editor => editor.Play.Value = p)));
           myHost.PerformModelAction(rd => rd.Pause.Advise(lifetime, p => UnityModel.Value.IfNotNull(editor => editor.Pause.Value = p)));
           myHost.PerformModelAction(rd => rd.Step.Advise(lifetime, () => UnityModel.Value.DoIfNotNull(editor => editor.Step())));
        }

        private void CreateProtocols(FileSystemPath protocolInstancePath)
        {
            if (!protocolInstancePath.ExistsFile)
                return;

            List<ProtocolInstance> protocolInstanceList;
            try
            {
                protocolInstanceList = ProtocolInstance.FromJson(protocolInstancePath.ReadAllText2().Text);
            }
            catch (Exception e)
            {
                myLogger.Warn($"Unable to parse {protocolInstancePath}" + Environment.NewLine + e);
                return;
            }

            var protocolInstance = protocolInstanceList?.SingleOrDefault(a => a.SolutionName == mySolution.SolutionFilePath.NameWithoutExtension);
            if (protocolInstance == null)
                return;

            myLogger.Info($"EditorPlugin protocol port {protocolInstance.Port} for Solution: {protocolInstance.SolutionName}.");

            if (protocolInstance.ProtocolGuid != ProtocolCompatibility.ProtocolGuid)
            {
                OnOutOfSync(myComponentLifetime);
                myLogger.Info("Avoid attempt to create protocol, incompatible.");
                return;
            }

            try
            {
                var lifetime = mySessionLifetimes.Next();
                myLogger.Info("Create protocol...");

                myLogger.Info("Creating SocketWire with port = {0}", protocolInstance.Port);
                var wire = new SocketWire.Client(lifetime, myDispatcher, protocolInstance.Port, "UnityClient");
                UnityWire.Value = wire;
                wire.BackwardsCompatibleWireFormat = true;

                var protocol = new Protocol("UnityEditorPlugin", new Serializers(),
                    new Identities(IdKind.Client), myDispatcher, wire, lifetime) {ThrowErrorOnOutOfSyncModels = false};

                protocol.OutOfSyncModels.AdviseOnce(lifetime, e =>
                {
                    if (myPluginInstallations.Contains(mySolution.SolutionFilePath))
                        return;

                    myPluginInstallations.Add(mySolution.SolutionFilePath); // avoid displaying Notification multiple times on each AppDomain.Reload in Unity

                    var appVersion = myUnityVersion.ActualVersionForSolution.Value;
                    if (appVersion < new Version(2019, 2))
                    {
                        var entry = myBoundSettingsStore.Schema.GetScalarEntry((UnitySettings s) => s.InstallUnity3DRiderPlugin);
                        var isEnabled = myBoundSettingsStore.GetValueProperty<bool>(lifetime, entry, null).Value;
                        if (!isEnabled)
                        {
                            myHost.PerformModelAction(model => model.OnEditorModelOutOfSync());
                        }
                    }
                    else
                    {
                        var notification = new NotificationModel("Advanced Unity integration is unavailable",
                            $"Please update External Editor to {myHostProductInfo.VersionMarketingString} in Unity Preferences.",
                            true, RdNotificationEntryType.WARN);
                        mySolution.Locks.ExecuteOrQueue(lifetime, "OutOfSyncModels.Notify", () => myNotificationsModel.Notification(notification));
                    }
                });

                protocol.OutOfSyncModels.AdviseOnce(lifetime, e => { OnOutOfSync(lifetime); });
                
                wire.Connected.WhenTrue(lifetime, lf =>
                {
                    myLogger.Info("WireConnected.");

                    var editor = new EditorPluginModel(lf, protocol);
                    editor.IsBackendConnected.Set(rdVoid => true);

                    if (PlatformUtil.RuntimePlatform == PlatformUtil.Platform.Windows)
                    {
                        var frontendProcess = Process.GetCurrentProcess().GetParent(); // RiderProcessId is not used on non-Windows, but this line gives bad warning in the log
                        if (frontendProcess != null)
                        {
                            editor.RiderProcessId.SetValue(frontendProcess.Id);
                        }
                    }

                    myHost.PerformModelAction(m => m.SessionInitialized.Value = true);

                    SubscribeToLogs(lf, editor);
                    SubscribeToOpenFile(editor);

                    editor.Play.Advise(lf, b => myHost.PerformModelAction(rd => rd.Play.SetValue(b)));
                    editor.Pause.Advise(lf, b => myHost.PerformModelAction(rd => rd.Pause.SetValue(b)));
                    editor.LastPlayTime.Advise(lf, time => myHost.PerformModelAction(rd => rd.LastPlayTime.SetValue(time)));
                    editor.LastInitTime.Advise(lf, time => myHost.PerformModelAction(rd => rd.LastInitTime.SetValue(time)));

                    editor.UnityProcessId.View(lf, (_, pid) => myHost.PerformModelAction(t => t.UnityProcessId.Set(pid)));

                    // I have split this into groups, because want to use async api for finding reference and pass them via groups to Unity
                    myHost.PerformModelAction(t => t.ShowFileInUnity.Advise(lf, v => editor.ShowFileInUnity.Fire(v)));
                    myHost.PerformModelAction(t => t.ShowPreferences.Advise(lf, v =>
                    {
                        editor.ShowPreferences.Fire();
                    }));

                    editor.EditorLogPath.Advise(lifetime,
                        s => myHost.PerformModelAction(a => a.EditorLogPath.SetValue(s)));
                    editor.PlayerLogPath.Advise(lifetime,
                        s => myHost.PerformModelAction(a => a.PlayerLogPath.SetValue(s)));

                    // Note that these are late-init properties. Once set, they are always set and do not allow nulls.
                    // This means that if/when the Unity <-> Backend protocol closes, they still retain the last value
                    // they had - so the front end will retain the log and application paths of the just-closed editor.
                    // Opening a new editor instance will reconnect and push a new value through to the front end
                    editor.UnityApplicationData.Advise(lifetime,
                        s => myHost.PerformModelAction(a =>
                        {
                            var version = UnityVersion.Parse(s.ApplicationVersion);
                            a.UnityApplicationData.SetValue(new UnityApplicationData(s.ApplicationPath,
                                    s.ApplicationContentsPath, s.ApplicationVersion, UnityVersion.RequiresRiderPackage(version)));
                        }));
                    editor.ScriptCompilationDuringPlay.Advise(lifetime,
                        s => myHost.PerformModelAction(a => a.ScriptCompilationDuringPlay.Set(ConvertToScriptCompilationEnum(s))));

                    myHost.PerformModelAction(rd =>
                    {
                        rd.GenerateUIElementsSchema.Set((l, u) =>
                            editor.GenerateUIElementsSchema.Start(l, u).ToRdTask(l));
                    });

                    editor.BuildLocation.Advise(lf, b => myHost.PerformModelAction(rd => rd.BuildLocation.SetValue(b)));

                    myHost.PerformModelAction(rd =>
                    {
                        rd.RunMethodInUnity.Set((l, data) =>
                        {
                            var editorRdTask = editor.RunMethodInUnity.Start(l, new RunMethodData(data.AssemblyName, data.TypeName, data.MethodName)).ToRdTask(l);
                            var frontendRes = new RdTask<JetBrains.Rider.Model.RunMethodResult>();

                            editorRdTask.Result.Advise(l, r =>
                            {
                                frontendRes.Set(new JetBrains.Rider.Model.RunMethodResult(r.Result.Success, r.Result.Message, r.Result.StackTrace));
                            });
                            return frontendRes;
                        });
                    });

                    TrackActivity(editor, lf);

                    if (!myComponentLifetime.IsTerminated)
                        myLocks.ExecuteOrQueueEx(myComponentLifetime, "setModel",
                            () => { UnityModel.SetValue(editor); });

                    lf.AddAction(() =>
                    {
                        if (!myComponentLifetime.IsTerminated)
                            myLocks.ExecuteOrQueueEx(myComponentLifetime, "clearModel", () =>
                            {
                                myLogger.Info("Wire disconnected.");
                                myHost.PerformModelAction(m => m.SessionInitialized.Value = false);
                                UnityModel.SetValue(null);
                            });
                    });
                });
            }
            catch (Exception ex)
            {
                myLogger.Error(ex);
            }
        }

        private void OnOutOfSync(Lifetime lifetime)
        {
            if (myPluginInstallations.Contains(mySolution.SolutionFilePath))
                return;

            myPluginInstallations.Add(mySolution
                .SolutionFilePath); // avoid displaying Notification multiple times on each AppDomain.Reload in Unity

            var appVersion = myUnityVersion.ActualVersionForSolution.Value;
            if (appVersion < new Version(2019, 2))
            {
                var entry = myBoundSettingsStore.Schema.GetScalarEntry((UnitySettings s) => s.InstallUnity3DRiderPlugin);
                var isEnabled = myBoundSettingsStore.GetValueProperty<bool>(lifetime, entry, null).Value;
                if (!isEnabled)
                {
                    myHost.PerformModelAction(model => model.OnEditorModelOutOfSync());
                }
            }
            else
            {
                var notification = new NotificationModel("Advanced Unity integration is unavailable",
                    $"Please update External Editor to {myHostProductInfo.VersionMarketingString} in Unity Preferences.",
                    true, RdNotificationEntryType.WARN);
                mySolution.Locks.ExecuteOrQueue(lifetime, "OutOfSyncModels.Notify",
                    () => myNotificationsModel.Notification(notification));
            }
        }

        private ScriptCompilationDuringPlay ConvertToScriptCompilationEnum(int mode)
        {
            if (mode < 0 || mode >= 3)
                return ScriptCompilationDuringPlay.RecompileAndContinuePlaying;

            return (ScriptCompilationDuringPlay) mode;
        }

        private void TrackActivity(EditorPluginModel editor, Lifetime lf)
        {
            editor.UnityApplicationData.AdviseOnce(lf, data => { myUsageStatistics.TrackActivity("UnityVersion", data.ApplicationVersion); });
            editor.ScriptingRuntime.AdviseOnce(lf, runtime => { myUsageStatistics.TrackActivity("ScriptingRuntime", runtime.ToString()); });
        }

        private void SubscribeToOpenFile([NotNull] EditorPluginModel editor)
        {
            editor.OpenFileLineCol.Set(args =>
            {
                var result = false;
                mySolution.Locks.ExecuteWithReadLock(() =>
                {
                    mySolution.GetComponent<IEditorManager>()
                        .OpenFile(FileSystemPath.Parse(args.Path), OpenFileOptions.DefaultActivate, myThreading,
                            textControl =>
                            {
                                var line = args.Line;
                                var column = args.Col;

                                if (line > 0 || column > 0) // avoid changing placement when it is not requested
                                {
                                    if (line > 0) line = line - 1;
                                    if (line < 0) line = 0;
                                    if (column > 0) column = column - 1;
                                    if (column < 0) column = 0;
                                    textControl.Caret.MoveTo((Int32<DocLine>) line,
                                        (Int32<DocColumn>) column,
                                        CaretVisualPlacement.Generic);
                                }

                                myHost.PerformModelAction(m =>
                                {
                                    m.ActivateRider();
                                    result = true;
                                });
                            },
                            () => { result = false; });
                });

                return result;
            });
        }

        private void SubscribeToLogs(Lifetime lifetime, EditorPluginModel editor)
        {
            editor.Log.Advise(lifetime, entry =>
            {
                myLogger.Verbose(entry.Time + " " + entry.Mode + " " + entry.Type + " " + entry.Message + " " + Environment.NewLine + " " + entry.StackTrace);
                var logEntry = new EditorLogEntry((int)entry.Type, (int)entry.Mode, entry.Time, entry.Message, entry.StackTrace);
                myHost.PerformModelAction(m => m.OnUnityLogEvent(logEntry));
            });
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    class ProtocolInstance
    {
        public readonly int Port;
        public readonly string SolutionName;
        public readonly Guid ProtocolGuid;

        public ProtocolInstance(int port, string solutionName, Guid protocolGuid)
        {
            Port = port;
            SolutionName = solutionName;
            ProtocolGuid = protocolGuid;
        }

        public static List<ProtocolInstance> FromJson(string json)
        {
            return JsonConvert.DeserializeObject<List<ProtocolInstance>>(json);
        }
    }
}