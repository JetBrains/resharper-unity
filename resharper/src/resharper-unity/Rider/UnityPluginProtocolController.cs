#if RIDER
using System;
using System.Threading;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Impl;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.Platform.Unity.Model;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features;
using JetBrains.Rider.Model;
using JetBrains.Threading;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityPluginProtocolController
    {
        public UnityModel UnityModel { get; private set; }
        private readonly IProperty<bool> myHostConnected = new Property<bool>("UnityHostConnected", false); 
        private readonly IProperty<bool> myPlay = new Property<bool>("UnityPlay", false);
        private readonly IProperty<bool> myHostConnectedAndPlay = new Property<bool>("UnityHostConnectedAndPlay", false);

        public UnityPluginProtocolController(Lifetime lifetime, ILogger logger, SolutionModel solutionModel, IScheduler dispatcher)
        {
            myHostConnectedAndPlay.Change.Advise(lifetime, args =>
            {
                if (args.HasNew && UnityModel != null) UnityModel.Play.Value = args.New;
            });

            solutionModel.GetCurrentSolution().CustomData
                .Data.Advise(lifetime, e =>
                {
                    if (e.Key == "UNITY_AttachEditorAndPlay")
                    {
                        if (e.NewValue != e.OldValue)
                        {
                            logger.Verbose($"UNITY_AttachEditorAndPlay {e.NewValue} came from frontend.");
                            myPlay.SetValue(e.NewValue.ToLower() == "true");

                            if (myHostConnected.Value)
                                myHostConnectedAndPlay.SetValue(myHostConnected.Value && myPlay.Value);
                        }
                    }
                });

            solutionModel.GetCurrentSolution().CustomData
                .Data.Advise(lifetime, e =>
                {
                    if (e.Key == "UNITY_ProcessId" && !string.IsNullOrEmpty(e.NewValue) && e.NewValue != e.OldValue)
                    {
                        var pid = Convert.ToInt32(e.NewValue);
                        logger.Verbose($"UNITY_ProcessId {e.NewValue} came from frontend.");

                        try
                        {
                            logger.Info("Unity process with Id = " + pid);

                            int port = 46000 + pid % 1000;

                            logger.Info("Create protocol...");
                            var protocol = new Protocol(new Serializers(), new Identities(IdKind.DynamicClient),
                                dispatcher,
                                creatingProtocol =>
                                {
                                    logger.Info("Creating SocketWire with port = {0}", port);
                                    return new SocketWire.Client(lifetime, creatingProtocol, port, "UnityClient");
                                });
                            UnityModel = new UnityModel(lifetime, protocol);
                            UnityModel.HostConnected.Advise(lifetime, b =>
                            {
                                myHostConnected.SetValue(b);
                                if (myPlay.Value)
                                    myHostConnectedAndPlay.SetValue(myPlay.Value);
                            });
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex);
                        }
                    }
                });
        }
    }
}
#endif