using System;
using System.IO;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.Platform.RdFramework.Impl;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.Platform.Unity.Model;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Tasks;
using JetBrains.ReSharper.Host.Features;
using JetBrains.Rider.Model;
using JetBrains.Util;
using Newtonsoft.Json;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityPluginProtocolController
    {
        private readonly Lifetime myLifetime;
        private readonly ILogger myLogger;
        private readonly SolutionModel mySolutionModel;
        private readonly IScheduler myDispatcher;
        private readonly IShellLocks myLocks;
        private readonly ISolution mySolution;
        private UnityModel UnityModel { get; set; }
        private readonly IProperty<bool> myPlay = new Property<bool>("UnityPlay", false);
        private readonly IProperty<bool> myServerConnectedAndPlay = new Property<bool>("UnityServerConnectedAndPlay", false)
            ;

        public UnityPluginProtocolController(Lifetime lifetime, ILogger logger, SolutionModel solutionModel,
            IScheduler dispatcher, IShellLocks locks, ISolutionLoadTasksScheduler solutionLoadTasksScheduler,
            ISolution solution)
        {
            myLifetime = lifetime;
            myLogger = logger;
            mySolutionModel = solutionModel;
            myDispatcher = dispatcher;
            myLocks = locks;
            mySolution = solution;

            solutionLoadTasksScheduler.EnqueueTask(new SolutionLoadTask("Init Unity Plugin Protocol",
                SolutionLoadTaskKinds.Done, Init));
        }

        private void Init()
        {
            myServerConnectedAndPlay.Change.Advise(myLifetime, args =>
            {
                if (args.HasNew && UnityModel != null) UnityModel.Play.Value = args.New;
            });

            var solFilePath = mySolution.SolutionFilePath;
            if (!ProjectExtensions.IsSolutionGeneratedByUnity(solFilePath.Directory))
                return;

            SubscribeToPlay(mySolutionModel.GetCurrentSolution());

            var protocolInstancePath =
                solFilePath.Directory.Combine(
                    "Library/ProtocolInstance.json"); // consider non-Unity Solution with Unity-generated projects

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
            watcher.Deleted += (sender, e) => { UnityModel?.ServerConnected.SetValue(false); };

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
                            myPlay.SetValue(e.NewValue.ToLower() == "true");

                            if (UnityModel != null && UnityModel.ServerConnected.HasValue() && UnityModel.ServerConnected.Value)
                                myServerConnectedAndPlay.SetValue(myPlay.Value);
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
                myLogger.Warn($"Unable to parse {protocolInstancePath}");
                myLogger.Warn(e);
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
                    if (myPlay.Value)
                        myServerConnectedAndPlay.SetValue(myPlay.Value);
                });
                UnityModel.ClientConnected.SetValue(true);
            }
            catch (Exception ex)
            {
                myLogger.Error(ex);
            }
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