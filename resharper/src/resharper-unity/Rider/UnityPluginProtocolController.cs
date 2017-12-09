using System;
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
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model;
using JetBrains.TextControl;
using JetBrains.TextControl.Coords.PositionKinds;
using JetBrains.Util;
using JetBrains.Util.dataStructures.TypedIntrinsics;
using Newtonsoft.Json;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityPluginProtocolController
    {
        private readonly Lifetime myLifetime;
        private readonly ILogger myLogger;
        private readonly IScheduler myDispatcher;
        private readonly IShellLocks myLocks;
        private readonly ISolution mySolution;
        private UnityModel UnityModel { get; set; }

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

            Init();
        }

        private void Init()
        {            
            SubscribeToPlay(mySolution.GetProtocolSolution());
            SubscribeRefresh(mySolution.GetProtocolSolution());

            var protocolInstancePath = mySolution.SolutionFilePath.Directory.Combine(
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
            watcher.Deleted += (sender, e) =>
            {
                myDispatcher.InvokeOrQueue(() => UnityModel?.ServerConnected.SetValue(false));
            };

            watcher.EnableRaisingEvents = true; // Begin watching.

            CreateProtocol(protocolInstancePath);
        }

        private void SubscribeToPlay(Solution solution)
        {
            solution.CustomData
                .Data.Advise(myLifetime, e =>
                {
                    if (e.Key == "UNITY_AttachEditorAndPlay")
                    {
                        if (e.NewValue != e.OldValue)
                        {
                            myLogger.Verbose($"UNITY_AttachEditorAndPlay {e.NewValue} came from frontend.");
                            UnityModel?.Play.SetValue(e.NewValue.ToLower() == "true");
                        }
                    }
                });

        }

        private void SubscribeRefresh(Solution solution)
        {
            solution.CustomData.Data.Advise(myLifetime, e =>
            {
                if (e.Key == "UNITY_Refresh")
                {
                    if (e.NewValue != e.OldValue && e.NewValue.ToLower() == "true")
                    {
                        myLogger.Verbose($"UNITY_Refresh {e.NewValue} came from frontend.");

                        if (UnityModel != null && UnityModel.ServerConnected.HasValue() &&
                            UnityModel.ServerConnected.Value)
                            UnityModel.Refresh.Start(RdVoid.Instance);
                        solution.CustomData.Data["UNITY_Refresh"] = "false";
                    }
                }
                
            });
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            var protocolInstancePath = FileSystemPath.Parse(e.FullPath);
            myLocks.ExecuteOrQueue(myLifetime, "CreateProtocol", ()=>CreateProtocol(protocolInstancePath));
        }

        private void CreateProtocol(FileSystemPath protocolInstancePath)
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
            
            myLogger.Verbose($"UNITY_Port {port}.");

            try
            {
                myLogger.Info("Create protocol...");
                var protocol = new Protocol(new Serializers(), new Identities(IdKind.DynamicClient), myDispatcher,
                    creatingProtocol =>
                    {
                        myLogger.Info("Creating SocketWire with port = {0}", port);
                        return new SocketWire.Client(myLifetime, creatingProtocol, port, "UnityClient");
                    });
                UnityModel = new UnityModel(myLifetime, protocol);
                UnityModel.ServerConnected.Advise(myLifetime, b =>
                {
                    myLogger.Info($"UnityModel.ServerConnected {b}");
                });
                UnityModel.IsClientConnected.Set(rdVoid => true);
                
                SubscribeToLogs();
                
                UnityModel.OpenFileLineCol.Set(args =>
                    {
                        using (ReadLockCookie.Create())
                        {
                            var textControl = mySolution.GetComponent<IEditorManager>().OpenFile(FileSystemPath.Parse(args.Path), OpenFileOptions.DefaultActivate);
                            if (textControl == null) 
                                return false;
                            if (args.Line > 0 || args.Col > 0)
                            {
                                textControl.Caret.MoveTo((Int32<DocLine>) (args.Line-1), (Int32<DocColumn>) args.Col, CaretVisualPlacement.Generic);
                            }    
                        }
                        return true;
                    });
            }
            catch (Exception ex)
            {
                myLogger.Error(ex);
            }
        }

        private void SubscribeToLogs()
        {
            UnityModel.LogModelInitialized.Advise(myLifetime, (modelInitialized) =>
            {
                modelInitialized.Log.Advise(myLifetime, entry =>
                {
                    switch (entry.Type)
                    {
                        case RdLogEventType.Error:
                        case RdLogEventType.Warning:
                            myLogger.Warn(entry.Message + Environment.NewLine + entry.StackTrace);
                            break;
                        case RdLogEventType.Message:
                            myLogger.Info(entry.Message + Environment.NewLine + entry.StackTrace);
                            break;
                    }
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