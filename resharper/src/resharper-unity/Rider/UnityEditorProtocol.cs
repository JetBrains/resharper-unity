using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.DataFlow.StandardPreconditions;
using JetBrains.DocumentModel;
using JetBrains.IDE;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.Platform.RdFramework.Impl;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Resources.Shell;
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
        private readonly Application.ActivityTrackingNew.UsageStatistics myUsageStatistics;
        private readonly PluginPathsProvider myPluginPathsProvider;
        private readonly UnityHost myHost;

        private readonly ReadonlyToken myReadonlyToken = new ReadonlyToken("unityModelReadonlyToken");
        public readonly Platform.RdFramework.Util.Signal<bool> Refresh = new Platform.RdFramework.Util.Signal<bool>();

        private readonly IProperty<EditorPluginModel> myUnityModel;
        private readonly IContextBoundSettingsStoreLive myBoundSettingsStore;

        [NotNull]
        public IProperty<EditorPluginModel> UnityModel => myUnityModel;

        public UnityEditorProtocol(Lifetime lifetime, ILogger logger, UnityHost host,
            IScheduler dispatcher, IShellLocks locks, ISolution solution, PluginPathsProvider pluginPathsProvider,
            ISettingsStore settingsStore, Application.ActivityTrackingNew.UsageStatistics usageStatistics,
            UnitySolutionTracker unitySolutionTracker)
        {
            myComponentLifetime = lifetime;
            myLogger = logger;
            myDispatcher = dispatcher;
            myLocks = locks;
            mySolution = solution;
            myPluginPathsProvider = pluginPathsProvider;
            myUsageStatistics = usageStatistics;
            myHost = host;
            myBoundSettingsStore = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()));
            mySessionLifetimes = new SequentialLifetimes(lifetime);
            myUnityModel = new Property<EditorPluginModel>(lifetime, "unityModelProperty", null)
                .EnsureReadonly(myReadonlyToken).EnsureThisThread();

            if (solution.GetData(ProjectModelExtensions.ProtocolSolutionKey) == null)
                return;

            unitySolutionTracker.IsUnityProject.ViewNotNull(lifetime, (lf, args) => 
            {
                if (!args) return;

                var solFolder = mySolution.SolutionFilePath.Directory;
                AdviseModelData(lifetime);

                // todo: consider non-Unity Solution with Unity-generated projects
                var protocolInstancePath = solFolder.Combine("Library/ProtocolInstance.json");
                protocolInstancePath.Directory.CreateDirectory();

                var watcher = new FileSystemWatcher();
                watcher.Path = protocolInstancePath.Directory.FullPath;
                watcher.NotifyFilter =
                    NotifyFilters.LastAccess |
                    NotifyFilters.LastWrite; //Watch for changes in LastAccess and LastWrite times
                watcher.Filter = protocolInstancePath.Name;

                // Add event handlers.
                watcher.Changed += OnChanged;
                watcher.Created += OnChanged;

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
            myHost.PerformModelAction(rd => rd.Play.AdviseNotNull(lifetime, p => UnityModel.Value.IfNotNull(editor => editor.Play.Value = p)));
            myHost.PerformModelAction(rd => rd.Pause.AdviseNotNull(lifetime, p => UnityModel.Value.IfNotNull(editor => editor.Pause.Value = p)));
            myHost.PerformModelAction(rd => rd.Step.Advise(lifetime, () => UnityModel.Value.DoIfNotNull(editor => editor.Step.Fire())));
            myHost.PerformModelAction(rd => rd.Refresh.Advise(lifetime, force => UnityModel.Value.IfNotNull(editor => editor.Refresh.Start(force))));
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
                        new Identities(IdKind.Client), myDispatcher, wire);
                    var editor = new EditorPluginModel(lf, protocol);
                    editor.IsBackendConnected.Set(rdVoid => true);
                    var frontendProcess = Process.GetCurrentProcess().GetParent();
                    if (frontendProcess != null)
                    {
                        editor.RiderProcessId.SetValue(frontendProcess.Id);
                    }

                    myHost.PerformModelAction(m => m.SessionInitialized.Value = true);

                    SubscribeToLogs(lf, editor);
                    SubscribeToOpenFile(editor);

                    editor.Play.AdviseNotNull(lf, b => myHost.PerformModelAction(rd => rd.Play.SetValue(b)));
                    editor.Pause.AdviseNotNull(lf, b => myHost.PerformModelAction(rd => rd.Pause.SetValue(b)));

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

                    BindPluginPathToSettings(lf, editor);

                    TrackActivity(editor, lf);

                    if (!myComponentLifetime.IsTerminated)
                        myLocks.ExecuteOrQueueEx(myComponentLifetime, "setModel",
                            () => { myUnityModel.SetValue(editor, myReadonlyToken); });

                    lf.AddAction(() =>
                    {
                        if (!myComponentLifetime.IsTerminated)
                            myLocks.ExecuteOrQueueEx(myComponentLifetime, "clearModel", () =>
                            {
                                myLogger.Info("Wire disconnected.");
                                myHost.PerformModelAction(m => m.SessionInitialized.Value = false);
                                myUnityModel.SetValue(null, myReadonlyToken);
                            });
                    });
                });
            }
            catch (Exception ex)
            {
                myLogger.Error(ex);
            }
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
                editor.ScriptingRuntime.AdviseNotNull(lf, runtime => { myUsageStatistics.TrackActivity("ScriptingRuntime", runtime.ToString()); });
            else
                myUsageStatistics.TrackActivity("ScriptingRuntime", editor.ScriptingRuntime.Value.ToString());
        }

        private void SubscribeToOpenFile([NotNull] EditorPluginModel editor)
        {
            editor.OpenFileLineCol.Set(args =>
            {
                using (ReadLockCookie.Create())
                {
                    var textControl = mySolution.GetComponent<IEditorManager>()
                        .OpenFile(FileSystemPath.Parse(args.Path), OpenFileOptions.DefaultActivate);
                    if (textControl == null)
                        return false;
                    if (args.Line > 0 || args.Col > 0)
                    {
                        textControl.Caret.MoveTo((Int32<DocLine>) (args.Line - 1), (Int32<DocColumn>) args.Col,
                            CaretVisualPlacement.Generic);
                    }
                }
                
                myHost.PerformModelAction(m => m.ActivateRider.Fire());
                return true;
            });
        }

        private void SubscribeToLogs(Lifetime lifetime, EditorPluginModel editor)
        {
            editor.Log.Advise(lifetime, entry =>
            {
                myLogger.Verbose(entry.Time + " " + entry.Mode + " " + entry.Type + " " + entry.Message + " " + Environment.NewLine + " " + entry.StackTrace);
                var logEntry = new EditorLogEntry((int)entry.Type, (int)entry.Mode, entry.Time, entry.Message, entry.StackTrace);
                myHost.PerformModelAction(m => m.OnUnityLogEvent.Fire(logEntry));
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
