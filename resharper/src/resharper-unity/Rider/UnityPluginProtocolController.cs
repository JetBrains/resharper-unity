using System;
using System.Diagnostics;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Impl;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features;
using JetBrains.Rider.Model;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{

    [SolutionComponent]
    public class UnityPluginProtocolController
    {
        public Protocol Protocol { get; private set; }

        public UnityPluginProtocolController(Lifetime lifetime, ILogger logger,  SolutionModel solutionModel)
        {
            solutionModel.GetCurrentSolution().CustomData
                .Data.Advise(lifetime, e =>
                {
                    if (e.Key == "UNITY_ProcessId" && e.NewValue != e.OldValue && !string.IsNullOrEmpty(e.NewValue))
                    {
                        var pid = Convert.ToInt32(e.NewValue);
                        logger.Verbose($"UNITY_ProcessId {e.NewValue} came from frontend.");

                        try
                        {
                            logger.Info("Unity process with Id = " + Process.GetCurrentProcess().Id);
                            logger.Info("Start ControllerTask...");

                            int port = 46000 + pid % 1000;
                            var dispatcher = new RdSimpleDispatcher(lifetime, logger);

                            logger.Info("Create protocol...");
                            Protocol = new Protocol(new Serializers(), new Identities(IdKind.DynamicClient), dispatcher,
                                creatingProtocol =>
                                {
                                    logger.Info("Creating SocketWire with port = {0}", port);
                                    return new SocketWire.Client(lifetime, creatingProtocol, port, "UnityClient");
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