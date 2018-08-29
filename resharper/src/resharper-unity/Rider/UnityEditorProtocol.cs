﻿using System;
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
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.dataStructures.TypedIntrinsics;
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
            myBoundSettingsStore =
                settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()));
            mySessionLifetimes = new SequentialLifetimes(lifetime);
            myUnityModel = new Property<EditorPluginModel>(lifetime, "unityModelProperty", null)
                .EnsureReadonly(myReadonlyToken).EnsureThisThread();

            if (!unitySolutionTracker.IsAbleToEstablishProtocolConnectionWithUnity.Value)
                return;

            if (solution.GetData(ProjectModelExtensions.ProtocolSolutionKey) == null)
                return;

            var solFolder = mySolution.SolutionFilePath.Directory;
            AdviseModelData(lifetime, mySolution.GetProtocolSolution());

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
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            var protocolInstancePath = FileSystemPath.Parse(e.FullPath);
            // connect on reload of server
            if (!myComponentLifetime.IsTerminated)
              myLocks.ExecuteOrQueue(myComponentLifetime, "CreateProtocol",
                () => CreateProtocols(protocolInstancePath));
        }

        private void AdviseModelData(Lifetime lifetime, Solution solution)
        {
            myHost.PerformModelAction(m => m.Play.Advise(lifetime, e =>
            {
                var model = UnityModel.Value;
                if (UnityModel.Value == null) return;
                if (model.Play.Value == e) return;

                myLogger.Info($"Play = {e} came from frontend.");
                model.Play.SetValue(e);

            }));

            myHost.PerformModelAction(m => m.Data.Advise(lifetime, e =>
            {
                var model = UnityModel.Value;
                if (e.NewValue == e.OldValue)
                    return;
                if (e.NewValue == null)
                    return;
                if (model==null)
                    return;

                switch (e.Key)
                {
                    case "UNITY_Refresh":
                        myLogger.Info($"{e.Key} = {e.NewValue} came from frontend.");
                        var force = Convert.ToBoolean(e.NewValue);
                        Refresh.Fire(force);
                        solution.CustomData.Data.Remove("UNITY_Refresh");
                        break;

                    case "UNITY_Step":
                        myLogger.Info($"{e.Key} = {e.NewValue} came from frontend.");
                        model.Step.Start(RdVoid.Instance);
                        solution.CustomData.Data.Remove("UNITY_Step");
                        break;

                    case "UNITY_Pause":
                        myLogger.Info($"{e.Key} = {e.NewValue} came from frontend.");
                        model.Pause.SetValue(Convert.ToBoolean(e.NewValue));
                        break;
                }
            }));
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

            myLogger.Info($"UNITY_Port {protocolInstance.Port} for Solution: {protocolInstance.SolutionName}.");

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
                    var model = new EditorPluginModel(lf, protocol);
                    model.IsBackendConnected.Set(rdVoid => true);
                    var frontendProcess = Process.GetCurrentProcess().GetParent();
                    if (frontendProcess != null)
                    {
                        model.RiderProcessId.SetValue(frontendProcess.Id);
                    }

                    myHost.SetModelData("UNITY_SessionInitialized", "true");

                    SubscribeToLogs(lf, model);
                    SubscribeToOpenFile(model);
                    model.Play.AdviseNotNull(lf, b => myHost.PerformModelAction(a=>a.Play.SetValue(b)));
                    model.Pause.AdviseNotNull(lf, b => myHost.SetModelData("UNITY_Pause", b.ToString().ToLower()));

                    // Note that these are late-init properties. Once set, they are always set and do not allow nulls.
                    // This means that if/when the Unity <-> Backend protocol closes, they still retain the last value
                    // they had - so the front end will retain the log and application paths of the just-closed editor.
                    // Opening a new editor instance will reconnect and push a new value through to the front end
                    model.EditorLogPath.Advise(lifetime,
                        s => myHost.PerformModelAction(a => a.EditorLogPath.SetValue(s)));
                    model.PlayerLogPath.Advise(lifetime,
                        s => myHost.PerformModelAction(a => a.PlayerLogPath.SetValue(s)));

                    model.ApplicationPath.Advise(lifetime,
                        s => myHost.PerformModelAction(a => a.ApplicationPath.SetValue(s)));
                    model.ApplicationContentsPath.Advise(lifetime,
                        s => myHost.PerformModelAction(a => a.ApplicationContentsPath.SetValue(s)));

                    BindPluginPathToSettings(lf, model);

                    TrackActivity(model, lf);

                    if (!myComponentLifetime.IsTerminated)
                        myLocks.ExecuteOrQueueEx(myComponentLifetime, "setModel",
                            () => { myUnityModel.SetValue(model, myReadonlyToken); });

                    lf.AddAction(() =>
                    {
                        if (!myComponentLifetime.IsTerminated)
                            myLocks.ExecuteOrQueueEx(myComponentLifetime, "clearModel", () =>
                            {
                                myLogger.Info("Wire disconnected.");
                                myHost.SetModelData("UNITY_SessionInitialized", "false");
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

        private void BindPluginPathToSettings(Lifetime lf, EditorPluginModel model)
        {
            var entry = myBoundSettingsStore.Schema.GetScalarEntry((UnitySettings s) => s.InstallUnity3DRiderPlugin);
            myBoundSettingsStore.GetValueProperty<bool>(lf, entry, null).Change.Advise(lf,
                val =>
                {
                    if (val.HasNew && val.New)
                        model.FullPluginPath.SetValue(myPluginPathsProvider.GetEditorPluginPathDir()
                            .Combine(PluginPathsProvider.FullPluginDllFile).FullPath);
                    model.FullPluginPath.SetValue(string.Empty);
                });
        }

        private void TrackActivity(EditorPluginModel model, Lifetime lf)
        {
            if (!model.ApplicationVersion.HasValue())
                model.ApplicationVersion.AdviseNotNull(lf, version => { myUsageStatistics.TrackActivity("UnityVersion", version); });
            else
                myUsageStatistics.TrackActivity("UnityVersion", model.ApplicationVersion.Value);
            if (!model.ScriptingRuntime.HasValue())
                model.ScriptingRuntime.AdviseNotNull(lf, runtime => { myUsageStatistics.TrackActivity("ScriptingRuntime", runtime.ToString()); });
            else
                myUsageStatistics.TrackActivity("ScriptingRuntime", model.ScriptingRuntime.Value.ToString());
        }

        private void SubscribeToOpenFile([NotNull] EditorPluginModel model)
        {
            model.OpenFileLineCol.Set(args =>
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

                myHost.SetModelData("UNITY_ActivateRider", "true");
                return true;
            });
        }

        private void SubscribeToLogs(Lifetime lifetime, EditorPluginModel model)
        {
            model.Log.Advise(lifetime, entry =>
            {
                myLogger.Verbose(entry.Time + " "+entry.Mode + " " + entry.Type + " " + entry.Message + " " + Environment.NewLine + " " + entry.StackTrace);
                myHost.SetModelData("UNITY_LogEntry", JsonConvert.SerializeObject(entry));
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
