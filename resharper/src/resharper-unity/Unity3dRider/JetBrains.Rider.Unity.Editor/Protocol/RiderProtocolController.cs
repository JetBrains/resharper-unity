using System;
using System.IO;
using System.Threading;
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
    public UnityModel Model;

    public RiderProtocolController(string dataPath, IScheduler mainThreadScheduler, Action<bool> playFunc,
      Action refresh, Lifetime lifetime)
    {
      mainThreadScheduler.Queue(() =>
      {
        var projectDirectory = Directory.GetParent(dataPath).FullName;
        var logger = Log.GetLog<RiderProtocolController>();
        logger.Verbose("InitProtocol");

        try
        {
          logger.Log(LoggingLevel.VERBOSE, "Start ControllerTask...");

          logger.Log(LoggingLevel.VERBOSE, "Create protocol...");
          
          var wire = new SocketWire.Server(lifetime, mainThreadScheduler, null, "UnityServer", true);
          logger.Log(LoggingLevel.VERBOSE, $"Creating SocketWire with port = {wire.Port}");
            
          wire.Connected.Advise(lifetime, clientIsConnected =>
          {
            logger.Verbose("wire.Connected {0}", clientIsConnected);
          });

          var protocol = new Protocol(new Serializers(), new Identities(IdKind.DynamicServer), mainThreadScheduler, wire);
                          
          InitializeProtocolJson(wire.Port, projectDirectory, logger);
          logger.Log(LoggingLevel.VERBOSE, "Create UnityModel and advise for new sessions...");

          Model = new UnityModel(lifetime, protocol);
          Model.Play.Advise(lifetime, play =>
          {
            logger.Log(LoggingLevel.VERBOSE, "model.Play.Advise: " + play);
            mainThreadScheduler.Queue(() => { playFunc(play); });
          });

          Model.LogModelInitialized.SetValue(new UnityLogModelInitialized());

          Model.Refresh.Set((l, x) =>
          {
            var task = new RdTask<RdVoid>();
            logger.Log(LoggingLevel.VERBOSE, "RiderPlugin.Refresh.");
            mainThreadScheduler.Queue(() =>
            {
              refresh();
              task.Set(RdVoid.Instance);
            });
            return task;
          });
        
          logger.Log(LoggingLevel.VERBOSE, "model.ServerConnected true.");
          Model.ServerConnected.SetValue(true);
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