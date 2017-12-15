using System;
using System.IO;
using System.Threading;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.Platform.RdFramework.Impl;
using JetBrains.Platform.RdFramework.Tasks;
using JetBrains.Platform.Unity.Model;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.Rider.Unity.Editor
{
  public class RiderProtocolController
  {
    public UnityModel Model;
    public Protocol myProtocol;

    public RiderProtocolController(ILog logger, string dataPath, IScheduler mainThreadScheduler, Action<bool> playFunc, Action refresh)
    {
      var projectDirectory = Directory.GetParent(dataPath).FullName;
      Log.DefaultFactory = new SingletonLogFactory(logger);
      logger.Verbose("InitProtocol");

      var lifetimeDefinition = Lifetimes.Define(EternalLifetime.Instance);
      var lifetime = lifetimeDefinition.Lifetime;
      AppDomain.CurrentDomain.DomainUnload += (EventHandler) ((_, __) => { lifetimeDefinition.Terminate(); });

      var thread = new Thread(() =>
      {
        try
        {
          logger.Log(LoggingLevel.VERBOSE, "Start ControllerTask...");

          var dispatcher = new SimpleInpaceExecutingScheduler(logger);
        
          logger.Log(LoggingLevel.VERBOSE, "Create protocol...");
          myProtocol = new Protocol(new Serializers(), new Identities(IdKind.DynamicServer), dispatcher,
            creatingProtocol =>
            {
              var wire = new SocketWire.Server(lifetime, creatingProtocol, null, "UnityServer");
              logger.Log(LoggingLevel.VERBOSE, $"Creating SocketWire with port = {wire.Port}");
            
              InitializeProtocolJson(wire.Port, projectDirectory, logger);
              return wire;
            });

          logger.Log(LoggingLevel.VERBOSE, "Create UnityModel and advise for new sessions...");
          
          Model = new UnityModel(lifetime, myProtocol);
          Model.Play.Advise(lifetime, play =>
          {
            logger.Log(LoggingLevel.VERBOSE, "model.Play.Advise: " + play);
            mainThreadScheduler.Queue(() => { playFunc(play); });
          });
          
          Model.LogModelInitialized.SetValue(new UnityLogModelInitialized());

          Model.Refresh.SetVoid(() =>
          {
            logger.Log(LoggingLevel.VERBOSE, "RiderPlugin.Refresh.");
            mainThreadScheduler.Queue(refresh);
          });
            
          logger.Log(LoggingLevel.VERBOSE, "model.ServerConnected true.");
          Model.ServerConnected.SetValue(true);
        }
        catch (Exception ex)
        {
          logger.Error(ex);
        }
      });
      thread.Start();
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


  /// <summary>
  /// Executes the given action just in the current thread in Queue method
  /// </summary>
  public class SimpleInpaceExecutingScheduler : IScheduler
  {
    private readonly ILog myLogger;
    public SimpleInpaceExecutingScheduler(ILog logger)
    {
      myLogger = logger;
    }

    public void Queue(Action action)
    {
      try
      {
        action();
      }
      catch (Exception ex)
      {
        myLogger.Error(ex);
      }
    }

    public bool IsActive => true;

    public bool OutOfOrderExecution => false;
  }
}