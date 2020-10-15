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
using JetBrains.IDE.UI.Extensions;
using JetBrains.Lifetimes;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.ProjectModel;
using JetBrains.Rd;
using JetBrains.Rd.Base;
using JetBrains.Rd.Impl;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Rider.Model.Notifications;
using JetBrains.Rider.Model.Unity;
using JetBrains.Rider.Unity.Editor.NonUnity;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.dataStructures.TypedIntrinsics;
using Newtonsoft.Json;
using UnityApplicationData = JetBrains.Rider.Model.Unity.FrontendBackend.UnityApplicationData;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityEditorProtocol
    {
        private readonly JetHashSet<FileSystemPath> myPluginInstallations;
        private readonly Lifetime myLifetime;
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
        private readonly FrontendBackendHost myHost;
        private readonly IContextBoundSettingsStoreLive myBoundSettingsStore;

        private DateTime myLastChangeTime;

        [NotNull]
        public readonly ViewableProperty<BackendUnityModel> BackendUnityModel = new ViewableProperty<BackendUnityModel>(null);

        public UnityEditorProtocol(Lifetime lifetime, ILogger logger, FrontendBackendHost host,
                                   IScheduler dispatcher, IShellLocks locks, ISolution solution,
                                   IApplicationWideContextBoundSettingStore settingsStore,
                                   JetBrains.Application.ActivityTrackingNew.UsageStatistics usageStatistics,
                                   UnitySolutionTracker unitySolutionTracker, IThreading threading,
                                   UnityVersion unityVersion, NotificationsModel notificationsModel,
                                   IHostProductInfo hostProductInfo, IFileSystemTracker fileSystemTracker)
        {
            myPluginInstallations = new JetHashSet<FileSystemPath>();

            myLifetime = lifetime;
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

                // todo: consider non-Unity Solution with Unity-generated projects
                var protocolInstancePath = solFolder.Combine("Library/ProtocolInstance.json");
                fileSystemTracker.AdviseFileChanges(lf, protocolInstancePath, OnProtocolInstanceJsonChange);

                // connect on start of Rider
                CreateProtocol(protocolInstancePath);
            });
        }

        private void SafeExecuteOrQueueEx(string name, Action action)
        {
            if (myLifetime.IsAlive) myLocks.ExecuteOrQueueEx(myLifetime, name, action);
        }

        private void OnProtocolInstanceJsonChange(FileSystemChangeDelta delta)
        {
            // Connect when protocols.json is updated (AppDomain start/reload in Unity editor)
            if (delta.ChangeType != FileSystemChangeType.ADDED && delta.ChangeType != FileSystemChangeType.CHANGED) return;
            if (!delta.NewPath.ExistsFile) return;
            if (delta.NewPath.FileModificationTimeUtc == myLastChangeTime) return;
            myLastChangeTime = delta.NewPath.FileModificationTimeUtc;

            SafeExecuteOrQueueEx("CreateProtocol", () => CreateProtocol(delta.NewPath));
        }

        private void CreateProtocol(FileSystemPath protocolInstancePath)
        {
            var protocolInstance = GetProtocolInstanceData(protocolInstancePath);
            if (protocolInstance == null)
                return;

            myLogger.Info($"EditorPlugin protocol port {protocolInstance.Port} for Solution: {protocolInstance.SolutionName}.");

            if (protocolInstance.ProtocolGuid != ProtocolCompatibility.ProtocolGuid)
            {
                OnOutOfSync(myLifetime);
                myLogger.Info("Avoid attempt to create protocol, incompatible.");
                return;
            }

            try
            {
                var thisSessionLifetime = mySessionLifetimes.Next();
                myLogger.Info("Create protocol...");

                myLogger.Info("Creating SocketWire with port = {0}", protocolInstance.Port);
                var wire = new SocketWire.Client(thisSessionLifetime, myDispatcher, protocolInstance.Port, "UnityClient")
                {
                    BackwardsCompatibleWireFormat = true
                };

                var protocol = new Rd.Impl.Protocol("UnityEditorPlugin", new Serializers(thisSessionLifetime, null, null),
                    new Identities(IdKind.Client), myDispatcher, wire, thisSessionLifetime)
                {
                    ThrowErrorOnOutOfSyncModels = false
                };

                protocol.OutOfSyncModels.AdviseOnce(thisSessionLifetime, _ => OnOutOfSync(thisSessionLifetime));

                wire.Connected.WhenTrue(thisSessionLifetime, connectionLifetime =>
                {
                    myLogger.Info("WireConnected.");

                    var backendUnityModel = new BackendUnityModel(connectionLifetime, protocol);

                    // Set up result for polling. Called before the Unity editor tries to use the protocol to open a
                    // file. It ensures that the protocol is connected and active.
                    // TODO: A property would be simpler
                    // This would also requires checking the model is still connected, as properties maintain state even
                    // when the connection has been lost
                    backendUnityModel.IsBackendConnected.Set(_ => true);

                    // TODO: Rename and move this to FrontendBackendHost
                    // This is telling the frontend that the BackendUnityModel is available
                    myHost.Do(frontendBackendModel => frontendBackendModel.SessionInitialized.Value = true);

                    AdviseModel(connectionLifetime, backendUnityModel);

                    SafeExecuteOrQueueEx("setModel", () => BackendUnityModel.SetValue(backendUnityModel));

                    connectionLifetime.OnTermination(() =>
                    {
                        SafeExecuteOrQueueEx("clearModel", () =>
                        {
                            myLogger.Info("Wire disconnected.");

                            // Tell the frontend the session is finished, and clear the model
                            // TODO: Move this to FrontendBackendHost
                            myHost.Do(m => m.SessionInitialized.Value = false);

                            // Clear model
                            BackendUnityModel.SetValue(null);
                        });
                    });
                });
            }
            catch (Exception ex)
            {
                myLogger.Error(ex);
            }
        }

        [CanBeNull]
        private ProtocolInstance GetProtocolInstanceData(FileSystemPath protocolInstancePath)
        {
            if (!protocolInstancePath.ExistsFile)
                return null;

            List<ProtocolInstance> protocolInstanceList;
            try
            {
                protocolInstanceList = ProtocolInstance.FromJson(protocolInstancePath.ReadAllText2().Text);
            }
            catch (Exception e)
            {
                myLogger.Warn($"Unable to parse {protocolInstancePath}" + Environment.NewLine + e);
                return null;
            }

            if (protocolInstanceList == null)
            {
                myLogger.Info("No protocols listed in ProtocolInstance.json");
                return null;
            }

            var protocolInstance = protocolInstanceList.SingleOrDefault(a =>
                a.SolutionName == mySolution.SolutionFilePath.NameWithoutExtension);
            if (protocolInstance == null)
            {
                myLogger.Info($"Cannot find a protocol for the current solution. {protocolInstanceList.Count} options");
            }

            return protocolInstance;
        }

        private void AdviseModel(Lifetime modelLifetime, BackendUnityModel backendUnityModel)
        {
            if (PlatformUtil.RuntimePlatform == PlatformUtil.Platform.Windows)
            {
                var frontendProcess =
                    Process.GetCurrentProcess()
                        .GetParent(); // RiderProcessId is not used on non-Windows, but this line gives bad warning in the log
                if (frontendProcess != null)
                {
                    backendUnityModel.RiderProcessId.SetValue(frontendProcess.Id);
                }
            }

            SubscribeToLogs(modelLifetime, backendUnityModel);
            SubscribeToOpenFile(backendUnityModel);

            backendUnityModel.Play.Advise(modelLifetime, b => myHost.Do(rd => rd.Play.SetValue(b)));
            backendUnityModel.Pause.Advise(modelLifetime, b => myHost.Do(rd => rd.Pause.SetValue(b)));
            backendUnityModel.LastPlayTime.Advise(modelLifetime, time => myHost.Do(rd => rd.LastPlayTime.SetValue(time)));
            backendUnityModel.LastInitTime.Advise(modelLifetime, time => myHost.Do(rd => rd.LastInitTime.SetValue(time)));

            backendUnityModel.UnityProcessId.View(modelLifetime, (_, pid) => myHost.Do(t => t.UnityProcessId.Set(pid)));

            // I have split this into groups, because want to use async api for finding reference and pass them via groups to Unity
            myHost.Do(t => t.ShowFileInUnity.Advise(modelLifetime, v => backendUnityModel.ShowFileInUnity.Fire(v)));
            myHost.Do(t => t.ShowPreferences.Advise(modelLifetime, v => { backendUnityModel.ShowPreferences.Fire(); }));

            backendUnityModel.EditorLogPath.Advise(modelLifetime, s => myHost.Do(a => a.EditorLogPath.SetValue(s)));
            backendUnityModel.PlayerLogPath.Advise(modelLifetime, s => myHost.Do(a => a.PlayerLogPath.SetValue(s)));

            // Note that these are late-init properties. Once set, they are always set and do not allow nulls.
            // This means that if/when the Unity <-> Backend protocol closes, they still retain the last value
            // they had - so the front end will retain the log and application paths of the just-closed editor.
            // Opening a new editor instance will reconnect and push a new value through to the front end
            backendUnityModel.UnityApplicationData.Advise(modelLifetime,
                s => myHost.Do(a =>
                {
                    var version = UnityVersion.Parse(s.ApplicationVersion);
                    a.UnityApplicationData.SetValue(new UnityApplicationData(s.ApplicationPath,
                        s.ApplicationContentsPath, s.ApplicationVersion, UnityVersion.RequiresRiderPackage(version)));
                }));
            myHost.Do(m =>
            {
                backendUnityModel.ScriptCompilationDuringPlay.FlowChangesIntoRd(modelLifetime, m.ScriptCompilationDuringPlay);
            });

            myHost.Do(rd =>
            {
                rd.GenerateUIElementsSchema.Set((l, u) =>
                    backendUnityModel.GenerateUIElementsSchema.Start(l, u).ToRdTask(l));
            });

            backendUnityModel.BuildLocation.Advise(modelLifetime, b => myHost.Do(rd => rd.BuildLocation.SetValue(b)));

            myHost.Do(rd =>
            {
                rd.RunMethodInUnity.Set((l, data) =>
                {
                    var editorRdTask = backendUnityModel.RunMethodInUnity.Start(l, data).ToRdTask(l);
                    var frontendRes = new RdTask<RunMethodResult>();

                    editorRdTask.Result.Advise(l, r => { frontendRes.Set(r.Result); });
                    return frontendRes;
                });
            });

            TrackActivity(backendUnityModel, modelLifetime);
        }

        private void OnOutOfSync(Lifetime lifetime)
        {
            if (myPluginInstallations.Contains(mySolution.SolutionFilePath))
                return;

            // avoid displaying Notification multiple times on each AppDomain.Reload in Unity
            myPluginInstallations.Add(mySolution.SolutionFilePath);

            var appVersion = myUnityVersion.ActualVersionForSolution.Value;
            if (appVersion < new Version(2019, 2))
            {
                var entry = myBoundSettingsStore.Schema.GetScalarEntry((UnitySettings s) => s.InstallUnity3DRiderPlugin);
                var isEnabled = myBoundSettingsStore.GetValueProperty<bool>(lifetime, entry, null).Value;
                if (!isEnabled)
                {
                    myHost.Do(model => model.OnEditorModelOutOfSync());
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

        private void TrackActivity(BackendUnityModel backendUnityModel, Lifetime lf)
        {
            backendUnityModel.UnityApplicationData.AdviseOnce(lf, data => myUsageStatistics.TrackActivity("UnityVersion", data.ApplicationVersion));
            backendUnityModel.ScriptingRuntime.AdviseOnce(lf, runtime => myUsageStatistics.TrackActivity("ScriptingRuntime", runtime.ToString()));
        }

        private void SubscribeToOpenFile([NotNull] BackendUnityModel backendUnityModel)
        {
            backendUnityModel.OpenFileLineCol.Set(args =>
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

                                myHost.Do(m =>
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

        private void SubscribeToLogs(Lifetime lifetime, BackendUnityModel backendUnityModel)
        {
            backendUnityModel.Log.Advise(lifetime, entry => myHost.Do(m => m.OnUnityLogEvent(entry)));
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