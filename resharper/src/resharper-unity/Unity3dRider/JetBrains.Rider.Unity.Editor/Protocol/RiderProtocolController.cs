using System;
using System.IO;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.Platform.RdFramework.Impl;
using JetBrains.Platform.RdFramework.Tasks;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.Platform.Unity.Model;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.Rider.Unity.Editor
{
  // ReSharper disable once UnusedMember.Global
  public class RiderProtocolController
  {
    public SocketWire.Server Wire;

    public RiderProtocolController(string dataPath, IScheduler mainThreadScheduler, Lifetime lifetime)
    {
      mainThreadScheduler.Queue(() =>
      {
        var projectDirectory = Directory.GetParent(dataPath).FullName;
        var logger = Log.GetLog<RiderProtocolController>();

        try
        {
          logger.Log(LoggingLevel.VERBOSE, "Start ControllerTask...");

          Wire = new SocketWire.Server(lifetime, mainThreadScheduler, null, "UnityServer", true);
          logger.Log(LoggingLevel.VERBOSE, $"Created SocketWire with port = {Wire.Port}");
          
          Wire.Connected.Advise(lifetime, wireConnected =>
          {
            logger.Verbose("Wire.Connected {0}", wireConnected);
          });
          InitializeProtocolJson(Wire.Port, projectDirectory, logger);
        }
        catch (Exception ex)
        {
          logger.Error("RiderProtocolController.ctor. " + ex);
        }
      }); 
    }

    private static void InitializeProtocolJson(int port, string projectDirectory, ILog logger)
    {
      logger.Verbose("Writing Library/ProtocolInstance.json");

      var library = Path.Combine(projectDirectory, "Library");
      var protocolInstanceJsonPath = Path.Combine(library, "ProtocolInstance.json");

      File.WriteAllText(protocolInstanceJsonPath, $@"{{""port_id"":{port}}}");

      AppDomain.CurrentDomain.DomainUnload += (sender, args) =>
      {
        logger.Verbose("Deleting Library/ProtocolInstance.json");
        File.Delete(protocolInstanceJsonPath);
      };
    }
  }
}