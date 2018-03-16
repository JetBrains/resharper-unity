using System;
using System.Diagnostics;
using System.IO;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.DataFlow.StandardPreconditions;
using JetBrains.DocumentModel;
using JetBrains.IDE;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.Platform.RdFramework.Impl;
using JetBrains.Platform.RdFramework.Tasks;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.Platform.Unity.Model;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features;
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
        private readonly Lifetime myLifetime;
        private readonly SequentialLifetimes mySessionLifetimes;
        private readonly ILogger myLogger;
        private readonly IScheduler myDispatcher;
        private readonly IShellLocks myLocks;
        private readonly ISolution mySolution;
        private readonly RiderUnityHost myHost;

        private readonly ReadonlyToken myReadonlyToken = new ReadonlyToken("unityModelReadonlyToken");
        public readonly ISignal<bool> Refresh = new DataFlow.Signal<bool>("Refresh");

        public UnityEditorProtocol(Lifetime lifetime, ILogger logger, RiderUnityHost host,
            IScheduler dispatcher, IShellLocks locks, ISolution solution)
        {
            myLifetime = lifetime;
            myLogger = logger;
            myDispatcher = dispatcher;
            myLocks = locks;
            mySolution = solution;
            myHost = host;
            mySessionLifetimes = new SequentialLifetimes(lifetime);
            UnityModel = new Property<UnityModel>(lifetime, "unityModelProperty", null).EnsureReadonly(myReadonlyToken).EnsureThisThread();
            
            if (!ProjectExtensions.IsSolutionGeneratedByUnity(solution.SolutionFilePath.Directory))
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
            CreateProtocol(protocolInstancePath, mySolution.GetProtocolSolution());
        }

        [NotNull]
        public IProperty<UnityModel> UnityModel { get; }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            var protocolInstancePath = FileSystemPath.Parse(e.FullPath);
            // connect on reload of server
            myLocks.ExecuteOrQueue(myLifetime, "CreateProtocol",
                () => CreateProtocol(protocolInstancePath, mySolution.GetProtocolSolution()));
        }

        private void AdviseModelData(Lifetime lifetime, Solution solution)
        {
            myHost.Model.Data.Advise(lifetime, e =>
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
                        Refresh.Fire(Convert.ToBoolean(e.NewValue));
                        break;
                    
                    case "UNITY_Step":
                        if (e.NewValue.ToLower() == true.ToString().ToLower())
                        {
                            myLogger.Info($"{e.Key} = {e.NewValue} came from frontend.");
                            model.Step.Start(RdVoid.Instance);
                        }
                        break;
                    
                    case "UNITY_Play":
                        myLogger.Info($"{e.Key} = {e.NewValue} came from frontend.");
                        model.Play.SetValue(Convert.ToBoolean(e.NewValue));
                        break;

                    case "UNITY_Pause":
                        myLogger.Info($"{e.Key} = {e.NewValue} came from frontend.");
                        model.Pause.SetValue(Convert.ToBoolean(e.NewValue));
                        break;
                }
            });
        }

        private void CreateProtocol(FileSystemPath protocolInstancePath, Solution solution)
        {
            int port;
            try
            {
                var protocolInstance =
                    JsonConvert.DeserializeObject<ProtocolInstance>(protocolInstancePath.ReadAllText2().Text);
                port = protocolInstance.port_id;
            }
            catch (Exception e)
            {
                myLogger.Warn($"Unable to parse {protocolInstancePath}" + Environment.NewLine + e);
                return;
            }

            myLogger.Info($"UNITY_Port {port}.");

            try
            {
                var lifetime = mySessionLifetimes.Next();
                myLogger.Info("Create protocol...");

                myLogger.Info("Creating SocketWire with port = {0}", port);
                var wire = new SocketWire.Client(lifetime, myDispatcher, port, "UnityClient");
                wire.Connected.WhenTrue(lifetime, lf =>
                {
                    myLogger.Info("WireConnected.");
                
                    var protocol = new Protocol("UnityEditorPlugin", new Serializers(), new Identities(IdKind.Client), myDispatcher, wire);
                    var model = new UnityModel(lf, protocol);
                    model.IsBackendConnected.Set(rdVoid => true);
                    model.RiderProcessId.SetValue(Process.GetCurrentProcess().Id);
                    myHost.SetModelData("UNITY_SessionInitialized", "true");

                    SubscribeToLogs(lf, model);
                    SubscribeToOpenFile(model);
                    model.Play.AdviseNotNull(lf, b => myHost.SetModelData("UNITY_Play", b.ToString().ToLower()));
                    model.Pause.AdviseNotNull(lf, b => myHost.SetModelData("UNITY_Pause", b.ToString().ToLower()));

                    myLocks.ExecuteOrQueueEx(myLifetime, "setModel",
                        () => { UnityModel.SetValue(model, myReadonlyToken); });
                    lf.AddAction(() =>
                    {
                        myLocks.ExecuteOrQueueEx(myLifetime, "clearModel", () =>
                        {
                            myLogger.Info("Wire disconnected.");
                            myHost.SetModelData("UNITY_SessionInitialized", "false");
                            UnityModel.SetValue(null, myReadonlyToken);
                        });
                    });
                });
            }
            catch (Exception ex)
            {
                myLogger.Error(ex);
            }
        }

        private void SubscribeToOpenFile([NotNull] UnityModel model)
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

        private void SubscribeToLogs(Lifetime lifetime, UnityModel model)
        {
            model.LogModelInitialized.Advise(lifetime, modelInitialized =>
            {
                modelInitialized.Log.Advise(lifetime, entry =>
                {
                    myLogger.Verbose(entry.Mode + " " + entry.Type + " " + entry.Message + " " + Environment.NewLine +
                                     " " + entry.StackTrace);
                    myHost.SetModelData("UNITY_LogEntry", JsonConvert.SerializeObject(entry));
                });
            });
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    class ProtocolInstance
    {
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public int port_id { get; set; }
    }
}