using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.DocumentModel;
using JetBrains.IDE;
using JetBrains.Lifetimes;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.Rd;
using JetBrains.Rd.Base;
using JetBrains.Rd.Impl;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.Rider.Model;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.dataStructures.TypedIntrinsics;
using JetBrains.Util.Special;
using Newtonsoft.Json;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityEditorProtocol
    {
        private readonly Lifetime myComponentLifetime;
        private readonly SequentialLifetimes mySessionLifetimes;
        private readonly ILogger myLogger;
        private readonly IScheduler myDispatcher;
        private readonly IShellLocks myLocks;
        private readonly ISolution mySolution;
        private readonly JetBrains.Application.ActivityTrackingNew.UsageStatistics myUsageStatistics;
        private readonly IThreading myThreading;
        private readonly PluginPathsProvider myPluginPathsProvider;
        private readonly UnityHost myHost;
        private readonly IContextBoundSettingsStoreLive myBoundSettingsStore;

        [NotNull]
        public readonly ViewableProperty<EditorPluginModel> UnityModel = new ViewableProperty<EditorPluginModel>(null);

        public UnityEditorProtocol(Lifetime lifetime, ILogger logger, UnityHost host,
            IScheduler dispatcher, IShellLocks locks, ISolution solution, PluginPathsProvider pluginPathsProvider,
            ISettingsStore settingsStore, JetBrains.Application.ActivityTrackingNew.UsageStatistics usageStatistics,
            UnitySolutionTracker unitySolutionTracker, IThreading threading)
        {
            myComponentLifetime = lifetime;
            myLogger = logger;
            myDispatcher = dispatcher;
            myLocks = locks;
            mySolution = solution;
            myPluginPathsProvider = pluginPathsProvider;
            myUsageStatistics = usageStatistics;
            myThreading = threading;
            myHost = host;
            myBoundSettingsStore = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()));
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
                protocolInstancePath.Directory.CreateDirectory();

                var watcher = new FileSystemWatcher();
                watcher.Path = protocolInstancePath.Directory.FullPath;
                watcher.NotifyFilter = NotifyFilters.LastWrite; //Watch for changes in LastWrite times
                watcher.Filter = protocolInstancePath.Name;

                // Add event handlers.
                watcher.Changed += OnChanged;
                watcher.Created += OnChanged;

                lf.Bracket(() => { }, () =>
                {
                    watcher.Dispose();
                });

                watcher.EnableRaisingEvents = true; // Begin watching.
                // connect on start of Rider
                CreateProtocols(protocolInstancePath);
            });
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            var protocolInstancePath = FileSystemPath.Parse(e.FullPath);
            // connect on reload of server
            if (!myComponentLifetime.IsTerminated)
                myLocks.ExecuteOrQueue(myComponentLifetime, "CreateProtocol",
                    () => CreateProtocols(protocolInstancePath));
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

            try
            {
                var lifetime = mySessionLifetimes.Next();
                myLogger.Info("Create protocol...");

                myLogger.Info("Creating SocketWire with port = {0}", protocolInstance.Port);
                var wire = new SocketWire.Client(lifetime, myDispatcher, protocolInstance.Port, "UnityClient");
                
                wire.Connected.WhenTrue(lifetime, lf =>
                {
                    myLogger.Info("WireConnected.");

                    var protocol = new Protocol("UnityEditorPlugin", new Serializers(),
                        new Identities(IdKind.Client), myDispatcher, wire, lf);

                    protocol.ThrowErrorOnOutOfSyncModels = false;
                    
                    protocol.OutOfSyncModels.Advise(lf, e =>
                    {
                        var entry = myBoundSettingsStore.Schema.GetScalarEntry((UnitySettings s) => s.InstallUnity3DRiderPlugin);
                        var isEnabled = myBoundSettingsStore.GetValueProperty<bool>(lf, entry, null).Value;
                        if (!isEnabled)
                        {
                            myHost.PerformModelAction(model => model.OnEditorModelOutOfSync());
                        }
                    });
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
                    editor.ClearOnPlay.Advise(lf, time => myHost.PerformModelAction(rd => rd.ClearOnPlay(time)));

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
                    editor.ApplicationPath.Advise(lifetime,
                        s => myHost.PerformModelAction(a => a.ApplicationPath.SetValue(s)));
                    editor.ApplicationContentsPath.Advise(lifetime,
                        s => myHost.PerformModelAction(a => a.ApplicationContentsPath.SetValue(s)));
                    editor.ScriptCompilationDuringPlay.Advise(lifetime,
                        s => myHost.PerformModelAction(a => a.ScriptCompilationDuringPlay.Set(ConvertToScriptCompilationEnum(s))));

                    BindPluginPathToSettings(lf, editor);

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

        private ScriptCompilationDuringPlay ConvertToScriptCompilationEnum(int mode)
        {
            if (mode < 0 || mode >= 3)
                return ScriptCompilationDuringPlay.RecompileAndContinuePlaying;

            return (ScriptCompilationDuringPlay) mode;
        }

        private void BindPluginPathToSettings(Lifetime lf, EditorPluginModel editor)
        {
            var entry = myBoundSettingsStore.Schema.GetScalarEntry((UnitySettings s) => s.InstallUnity3DRiderPlugin);
            myBoundSettingsStore.GetValueProperty<bool>(lf, entry, null).Change.Advise(lf,
                val =>
                {
                    if (val.HasNew && val.New)
                        editor.FullPluginPath.SetValue(myPluginPathsProvider.GetEditorPluginPathDir()
                            .Combine(PluginPathsProvider.FullPluginDllFile).FullPath);
                    editor.FullPluginPath.SetValue(string.Empty);
                });
        }

        private void TrackActivity(EditorPluginModel editor, Lifetime lf)
        {
            if (!editor.ApplicationVersion.HasValue())
                editor.ApplicationVersion.AdviseNotNull(lf, version => { myUsageStatistics.TrackActivity("UnityVersion", version); });
            else
                myUsageStatistics.TrackActivity("UnityVersion", editor.ApplicationVersion.Value);
            if (!editor.ScriptingRuntime.HasValue())
                editor.ScriptingRuntime.Advise(lf, runtime => { myUsageStatistics.TrackActivity("ScriptingRuntime", runtime.ToString()); });
            else
                myUsageStatistics.TrackActivity("ScriptingRuntime", editor.ScriptingRuntime.Value.ToString());
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

        public ProtocolInstance(int port, string solutionName)
        {
            Port = port;
            SolutionName = solutionName;
        }

        public static List<ProtocolInstance> FromJson(string json)
        {
            return JsonConvert.DeserializeObject<List<ProtocolInstance>>(json);
        }
    }
}