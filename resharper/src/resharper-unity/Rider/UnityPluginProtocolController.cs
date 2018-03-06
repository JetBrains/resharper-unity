using System;
using System.Diagnostics;
using System.IO;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
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
    public class UnityPluginProtocolController
    {
        private readonly Lifetime myLifetime;
        private readonly SequentialLifetimes mySessionLifetimes;
        private readonly ILogger myLogger;
        private readonly IScheduler myDispatcher;
        private readonly IShellLocks myLocks;
        private readonly ISolution mySolution;
        [CanBeNull] public UnityModel UnityModel;

        public readonly ISignal<UnityModel> Refresh = new DataFlow.Signal<UnityModel>("Refresh");

        public UnityPluginProtocolController(Lifetime lifetime, ILogger logger, 
            IScheduler dispatcher, IShellLocks locks, ISolution solution)
        {
            if (!ProjectExtensions.IsSolutionGeneratedByUnity(solution.SolutionFilePath.Directory))
                return;

            myLifetime = lifetime;
            myLogger = logger;
            myDispatcher = dispatcher;
            myLocks = locks;
            mySolution = solution;
            mySessionLifetimes = new SequentialLifetimes(lifetime);
            
            if (solution.GetData<Solution>(ProjectModelExtensions.ProtocolSolutionKey) == null)
                return;

            var solFolder = mySolution.SolutionFilePath.Directory;
            AdviseCustomDataFromFrontend(lifetime, mySolution.GetProtocolSolution());

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
        
        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            var protocolInstancePath = FileSystemPath.Parse(e.FullPath);
            // connect on reload of server
            myLocks.ExecuteOrQueue(myLifetime, "CreateProtocol", ()=> CreateProtocol(protocolInstancePath, mySolution.GetProtocolSolution()));
        }

        private void AdviseCustomDataFromFrontend(Lifetime lifetime, Solution solution)
        {
            solution.CustomData.Data.Advise(lifetime, e =>
            {
                var model = UnityModel;
                if (e.NewValue == e.OldValue || e.NewValue == null) 
                    return;
                switch (e.Key)
                {
                    case "UNITY_Refresh":
                        if (e.NewValue.ToLower() == true.ToString().ToLower())
                        {
                            myLogger.Info($"{e.Key} = {e.NewValue} came from frontend.");
                            if (model!=null)
                                Refresh.Fire(model);
                        }

                        break;
                    case "UNITY_Step":
                        if (e.NewValue.ToLower() == true.ToString().ToLower())
                        {
                            myLogger.Info($"{e.Key} = {e.NewValue} came from frontend.");
                            model?.Step.Sync(RdVoid.Instance, RpcTimeouts.Default);
                        }

                        break;
                    case "UNITY_Play":
                        myLogger.Info($"{e.Key} = {e.NewValue} came from frontend.");
                        model?.Play.SetValue(Convert.ToBoolean(e.NewValue));
                        break;

                    case "UNITY_Pause":
                        myLogger.Info($"{e.Key} = {e.NewValue} came from frontend.");
                        model?.Pause.SetValue(Convert.ToBoolean(e.NewValue));
                        break;
                }
            });
        }

        private void CreateProtocol(FileSystemPath protocolInstancePath, Solution solution)
        {
            int port;
            try
            {
                var protocolInstance = JsonConvert.DeserializeObject<ProtocolInstance>(protocolInstancePath.ReadAllText2().Text);
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
                
                    var protocol = new Protocol(new Serializers(), new Identities(IdKind.Client), myDispatcher, wire);
                    var model = new UnityModel(lf, protocol);
                    model.IsClientConnected.Set(rdVoid => true);
                    model.RiderProcessId.SetValue(Process.GetCurrentProcess().Id);
                    SetOrCreateDataKeyValuePair(solution, "UNITY_SessionInitialized", "true");
                    
                    SubscribeToLogs(lf, model, solution);
                    SubscribeToOpenFile(model, solution);
                    model.Play.AdviseNotNull(lf, b => SetOrCreateDataKeyValuePair(solution, "UNITY_Play", b.ToString().ToLower()));
                    model.Pause.AdviseNotNull(lf, b => SetOrCreateDataKeyValuePair(solution, "UNITY_Pause", b.ToString().ToLower()));
                    
                    UnityModel = model;
                    lf.AddAction(() =>
                    {
                        myLogger.Info("Wire disconnected.");
                        SetOrCreateDataKeyValuePair(solution, "UNITY_SessionInitialized", "false");
                        UnityModel = null;
                    });
                });
            }
            catch (Exception ex)
            {
                myLogger.Error(ex);
            }
        }

        private void SubscribeToOpenFile([NotNull] UnityModel model, Solution solution)
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
                        textControl.Caret.MoveTo((Int32<DocLine>) (args.Line - 1), (Int32<DocColumn>) args.Col, CaretVisualPlacement.Generic);
                    }
                }

                SetOrCreateDataKeyValuePair(solution, "UNITY_ActivateRider", "true");
                return true;
            });
        }

        private static void SetOrCreateDataKeyValuePair(Solution solution, string key, string value)
        {
            var data = solution.CustomData.Data;
            if (data.ContainsKey(key))
                data[key] = value;
            else
                data.Add(key, value);
        }

        private void SubscribeToLogs(Lifetime lifetime, UnityModel model, Solution solution)
        {
            model.LogModelInitialized.Advise(lifetime, modelInitialized =>
            {
                modelInitialized.Log.Advise(lifetime, entry =>
                {
                    myLogger.Verbose(entry.Mode +" " + entry.Type +" "+ entry.Message +" "+ Environment.NewLine +" "+ entry.StackTrace);
                    SetOrCreateDataKeyValuePair(solution, "UNITY_LogEntry", JsonConvert.SerializeObject(entry));
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