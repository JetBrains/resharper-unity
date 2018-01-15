using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.DocumentModel;
using JetBrains.IDE;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.Platform.RdFramework.Impl;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.Platform.Unity.Model;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Host.Features.BackgroundTasks;
using JetBrains.ReSharper.Host.Features.FileSystem;
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
        private readonly SequentialLifetimes SessionLifetimes;
        private readonly ILogger myLogger;
        private readonly IScheduler myDispatcher;
        private readonly IShellLocks myLocks;
        private readonly ISolution mySolution;
        private UnityModel UnityModel;
        private Protocol myProtocol;

        public ISignal<Void> Refresh = new DataFlow.Signal<Void>("Refresh");

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
            SessionLifetimes = new SequentialLifetimes(lifetime);

            var solFolder = mySolution.SolutionFilePath.Directory;
            Advise(mySolution.GetProtocolSolution());

            var protocolInstancePath = solFolder.Combine(
                "Library/ProtocolInstance.json"); // todo: consider non-Unity Solution with Unity-generated projects

            if (!protocolInstancePath.ExistsFile)
                File.Create(protocolInstancePath.FullPath);

            FileSystemWatcher watcher = new FileSystemWatcher();
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

        private void Advise(Solution solution)
        {
            solution.CustomData.Data.Advise(myLifetime, e =>
            {
                if (e.NewValue == e.OldValue || e.NewValue == null) return;
                switch (e.Key)
                {
                    case "UNITY_Refresh":
                        if (e.NewValue.ToLower() == "true")
                        {
                            myLogger.Info($"UNITY_Refresh {e.NewValue} came from frontend.");
                            if (UnityModel != null) 
                              Refresh.Fire(UnityModel);    
                        }
                        break;
                    case "UNITY_Play":
                        myLogger.Info($"UNITY_Play {e.NewValue} came from frontend.");
                        UnityModel?.Play.SetValue(e.NewValue.ToLower() == "true");
                        break;  
                    default:
                        throw new NotImplementedException($"Unhandled {e.Key}= {e.NewValue} came from frontend.");
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
                myLogger.Info("Create protocol...");
                var lifetime = SessionLifetimes.Next();
                myProtocol = new Protocol(new Serializers(), new Identities(IdKind.DynamicClient), myDispatcher,
                    creatingProtocol =>
                    {
                        myLogger.Info("Creating SocketWire with port = {0}", port);
                        return new SocketWire.Client(lifetime, creatingProtocol, port, "UnityClient");
                    });
                UnityModel = new UnityModel(lifetime, myProtocol);
                UnityModel.ServerConnected.Advise(lifetime, b =>
                {
                    myLogger.Info($"UnityModel.ServerConnected {b}");
                });
                UnityModel.IsClientConnected.Set(rdVoid => true);
                UnityModel.RiderProcessId.SetValue(Process.GetCurrentProcess().Id);
                SetOrCreateDataKeyValuePair(solution, "UNITY_SessionInitialized", "true");
                
                SubscribeToLogs(lifetime, solution);
                SubscribeToOpenFile(solution);
            }
            catch (Exception ex)
            {
                myLogger.Error(ex);
            }
        }

        private void SubscribeToOpenFile(Solution solution)
        {
            UnityModel.OpenFileLineCol.Set(args =>
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

        private void SubscribeToLogs(Lifetime lifetime, Solution solution)
        {
            UnityModel.LogModelInitialized.Advise(lifetime, modelInitialized =>
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