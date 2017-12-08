using System;
using System.IO;
using UnityEditor;
using System.Reflection;
using JetBrains.Platform.RdFramework.Tasks;
using JetBrains.Platform.RdFramework.Util;
using System.Threading;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.Platform.RdFramework.Impl;
using JetBrains.Platform.Unity.Model;
using JetBrains.Util;
using JetBrains.Util.Logging;
using Plugins.Editor.JetBrains;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using ILog = JetBrains.Util.Logging.ILog;
using IScheduler = JetBrains.Platform.RdFramework.IScheduler;

// ReSharper disable RedundantArgumentDefaultValue

namespace Plugins.Editor.JetBrains
{
  [InitializeOnLoad]
  public static class RiderProtocolController
  {
    public static bool Initialized { get; private set; }
    public static readonly string logPath = Path.Combine(Path.Combine(Path.GetTempPath(), "Unity3dRider"), DateTime.Now.ToString("yyyy-MM-ddT-HH-mm-ss") + ".log");
    
    public static UnityModel model;
    private static Protocol ourProtocol;
    
    static RiderProtocolController()
    {
      if (!RiderPlugin1.Enabled)
        return;
      Debug.Log(string.Format("Rider plugin initialized. Further logs in: {0}", logPath));
      
      InitProtocol();
            
      EventInfo eventInfo = typeof (Application).GetEvent("logMessageReceived", BindingFlags.Static | BindingFlags.Public);
      if (eventInfo != null)
      {
        eventInfo.AddEventHandler(null, new Application.LogCallback(ApplicationOnLogMessageReceived));
        AppDomain.CurrentDomain.DomainUnload += (EventHandler) ((_, __) =>
        {
          eventInfo.RemoveEventHandler(null, new Application.LogCallback(ApplicationOnLogMessageReceived));
        });
      }
      else
      {
        Application.RegisterLogCallback(ApplicationOnLogMessageReceived);
      }
    }

    private static void InitProtocol()
    {
      var projectDirectory = Directory.GetParent(Application.dataPath).FullName;
      
      var logger = new RiderLogger();
      Log.DefaultFactory = new SingletonLogFactory(logger);
      logger.Verbose("InitProtocol");

      var lifetimeDefinition = Lifetimes.Define(EternalLifetime.Instance, null, null, null); // do not remove default params to compile in Unity 5.3
      var lifetime = lifetimeDefinition.Lifetime;

      var thread = new Thread(() =>
      {
        try
        {
          logger.Log(LoggingLevel.VERBOSE, "Start ControllerTask...");

          var dispatcher = new SimpleInpaceExecutingScheduler(logger);
        
          logger.Log(LoggingLevel.VERBOSE, "Create protocol...");
          ourProtocol = new Protocol(new Serializers(), new Identities(IdKind.DynamicServer), dispatcher,
            creatingProtocol =>
            {
              var wire = new SocketWire.Server(lifetime, creatingProtocol, null, "UnityServer");
              logger.Log(LoggingLevel.VERBOSE, string.Format("Creating SocketWire with port = {0}", wire.Port));
            
              InitializeProtocolJson(wire.Port, projectDirectory, logger);
              return wire;
            });

          logger.Log(LoggingLevel.VERBOSE, "Create UnityModel and advise for new sessions...");
          
          model = new UnityModel(lifetime, ourProtocol);
          model.Play.Advise(lifetime, play =>
          {
            logger.Log(LoggingLevel.VERBOSE, "model.Play.Advise: " + play);
            MainThreadDispatcher.Queue(() =>
            {
              EditorApplication.isPlaying = play;
            });
          });
          
          model.LogModelInitialized.SetValue(new UnityLogModelInitialized());

          model.Refresh.Set((lifetime1, vo) =>
          {
            logger.Log(LoggingLevel.VERBOSE, "RiderPlugin.Refresh.");
            MainThreadDispatcher.Queue(AssetDatabase.Refresh);
            return new RdTask<RdVoid>();
          });
               
          logger.Log(LoggingLevel.VERBOSE, "model.ServerConnected true.");
          model.ServerConnected.SetValue(true);
        }
        catch (Exception ex)
        {
          logger.Error(ex);
        }
      });
      thread.Start();
      Initialized = true;
    }

    private static void InitializeProtocolJson(int port, string projectDirectory, ILog logger)
    {
      logger.Verbose("Writing Library/ProtocolInstance.json");

      var library = Path.Combine(projectDirectory, "Library");
      var protocolInstanceJsonPath = Path.Combine(library, "ProtocolInstance.json");

      File.WriteAllText(protocolInstanceJsonPath, string.Format(@"{{""port_id"":{0}}}", port));

      AppDomain.CurrentDomain.DomainUnload += (sender, args) =>
      {
        logger.Verbose("Deleting Library/ProtocolInstance.json");
        File.Delete(protocolInstanceJsonPath);
      };
    }

    private static void ApplicationOnLogMessageReceived(string message, string stackTrace, UnityEngine.LogType type)
    {
      if (RiderPlugin1.SendConsoleToRider)
      {
        if (ourProtocol == null)
          return;
        // use Protocol to pass log entries to Rider
        ourProtocol.Scheduler.InvokeOrQueue(() =>
        {
          if (model != null && model.LogModelInitialized.HasValue())
          {
            switch (type)
            {
              case LogType.Error:
              case LogType.Exception:
                SentLogEvent(message, stackTrace, RdLogEventType.Error);
                break;
              case LogType.Warning:
                SentLogEvent(message, stackTrace, RdLogEventType.Warning);
                break;
              default:
                SentLogEvent(message, stackTrace, RdLogEventType.Message);
                break;
            }
          }
        });
      }
    }

    private static void SentLogEvent(string message, string stackTrace, RdLogEventType type)
    {
      if (!message.StartsWith("[Rider][TRACE]")) // avoid sending because in Trace mode log about sending log event to Rider, will also appear in unity log
        model.LogModelInitialized.Value.Log.Fire(new RdLogEvent(type, message, stackTrace));
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

    public bool IsActive
    {
      get { return true; }
    }

    public bool OutOfOrderExecution
    {
      get { return false; }
    }
  }
}