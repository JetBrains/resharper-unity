using System;
using System.IO;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Impl;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.Rider.Unity.Editor.NonUnity
{
  // ReSharper disable once UnusedMember.Global
  public class RiderProtocolController
  {
    public SocketWire.Server Wire;

    public RiderProtocolController(IScheduler mainThreadScheduler, Lifetime lifetime)
    {
      var logger = Log.GetLog<RiderProtocolController>();

      try
      {
        logger.Log(LoggingLevel.VERBOSE, "Start ControllerTask...");

        Wire = new SocketWire.Server(lifetime, mainThreadScheduler, null, "UnityServer", true);
        logger.Log(LoggingLevel.VERBOSE, $"Created SocketWire with port = {Wire.Port}");

        Wire.Connected.Advise(lifetime, wireConnected => { logger.Verbose("Wire.Connected {0}", wireConnected); });
        InitializeProtocolJson(Wire.Port, logger);
      }
      catch (Exception ex)
      {
        logger.Error("RiderProtocolController.ctor. " + ex);
      }
    }

    private static void InitializeProtocolJson(int port, ILog logger)
    {
      logger.Verbose("Writing Library/ProtocolInstance.json");

      var protocolInstanceJsonPath = Path.GetFullPath("Library/ProtocolInstance.json");

      File.WriteAllText(protocolInstanceJsonPath, $@"{{""port_id"":{port}}}");

      AppDomain.CurrentDomain.DomainUnload += (sender, args) =>
      {
        logger.Verbose("Deleting Library/ProtocolInstance.json");
        File.Delete(protocolInstanceJsonPath);
      };
    }
  }
}