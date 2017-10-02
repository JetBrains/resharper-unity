#if RIDER
using System;
using System.Diagnostics;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Impl;
using JetBrains.Platform.Unity.Model;
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
                    if (e.Key == "UNITY_AttachEditorAndRun" && e.NewValue.ToLower()=="true" && e.NewValue!=e.OldValue)
                    {
                        logger.Verbose($"UNITY_AttachEditorAndRun {e.NewValue} came from frontend.");
                        var model = new UnityModel(lifetime, Protocol);
                        model.Play.Value = true;
                    }
                    
                    if (e.Key == "UNITY_ProcessId" && !string.IsNullOrEmpty(e.NewValue) && (e.NewValue != e.OldValue || Protocol==null) )
                    {
                        var pid = Convert.ToInt32(e.NewValue);
                        logger.Verbose($"UNITY_ProcessId {e.NewValue} came from frontend.");

                        try
                        {
                            logger.Info("Unity process with Id = " + pid);

                            int port = 46000 + pid % 1000;
                            var dispatcher = new RdSimpleDispatcher(lifetime, logger);

                            logger.Info("Create protocol...");
                            Protocol = new Protocol(new Serializers(), new Identities(IdKind.DynamicClient), dispatcher,
                                creatingProtocol =>
                                {
                                    logger.Info("Creating SocketWire with port = {0}", port);
                                    return new SocketWire.Client(lifetime, creatingProtocol, port, "UnityClient");
                                });
                            logger.Info("Run dispatcher...");
                            dispatcher.Run();
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