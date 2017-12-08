using System;
using System.IO;
using UnityEditor;
using System.Collections.Generic;
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
using UnityEngine;
using ILog = JetBrains.Util.Logging.ILog;

namespace Plugins.Editor.JetBrains
{
  [InitializeOnLoad]
  public static class RiderProtocolController
  {
    public static bool Initialized { get; private set; }

    public static UnityModel model;
    private static Protocol ourProtocol;

    
    static RiderProtocolController()
    {
      if (!RiderPlugin.Enabled)
        return;

      InitProtocol();
      
      #if UNITY_5_5_OR_NEWER
      Application.logMessageReceivedThreaded+=ApplicationOnLogMessageReceived; // not supported in Unity 4.7
      #else
      Application.RegisterLogCallback(ApplicationOnLogMessageReceived);
      #endif
    }

    private static void InitProtocol()
    {
      var projectDirectory = Directory.GetParent(Application.dataPath).FullName;
      
      var logger = new RiderLogger();
      Log.DefaultFactory = new SingletonLogFactory(logger);
      logger.Verbose("InitProtocol");

      var lifetimeDefinition = Lifetimes.Define(EternalLifetime.Instance);
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
      if (RiderPlugin.SendConsoleToRider)
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

    [InitializeOnLoad]
    internal static class MainThreadDispatcher
    {
      private struct Task
      {
        public readonly Delegate Function;
        public readonly object[] Arguments;

        public Task(Delegate function, object[] arguments)
        {
          Function = function;
          Arguments = arguments;
        }
      }

      /// <summary>
      /// The queue of tasks that are being requested for the next time DispatchTasks is called
      /// </summary>
      private static Queue<Task> mTaskQueue = new Queue<Task>();

      /// <summary>
      /// Indicates whether there are tasks available for dispatching
      /// </summary>
      /// <value>
      /// <c>true</c> if there are tasks available for dispatching; otherwise, <c>false</c>.
      /// </value>
      private static bool AreTasksAvailable
      {
        get { return mTaskQueue.Count > 0; }
      }

      /// <summary>
      /// Initializes all the required callbacks for this class to work properly
      /// </summary>
      static MainThreadDispatcher()
      {
        if (!RiderPlugin.Enabled)
          return;
        
#if UNITY_EDITOR
        EditorApplication.update += DispatchTasks;
#endif
      }
      
      /// <summary>
      /// Dispatches the specified action delegate.
      /// </summary>
      /// <param name='function'>
      /// The function delegate being requested
      /// </param>
      public static void Queue(Action function)
      {
        Queue(function, null);
      }

      /// <summary>
      /// Dispatches the specified function delegate with the desired delegates
      /// </summary>
      /// <param name='function'>
      /// The function delegate being requested
      /// </param>
      /// <param name='arguments'>
      /// The arguments to be passed to the function delegate
      /// </param>
      /// <exception cref='System.NotSupportedException'>
      /// Is thrown when this method is called from the Unity Player
      /// </exception>
      private static void Queue(Delegate function, params object[] arguments)
      {
#if UNITY_EDITOR
        lock (mTaskQueue)
        {
          mTaskQueue.Enqueue(new Task(function, arguments));
        }
#else
		throw new System.NotSupportedException("Dispatch is not supported in the Unity Player!");
#endif
      }

      /// <summary>
      /// Dispatches the tasks that has been requested since the last call to this function
      /// </summary>
      /// <exception cref='System.NotSupportedException'>
      /// Is thrown when this method is called from the Unity Player
      /// </exception>
      private static void DispatchTasks()
      {
#if UNITY_EDITOR
        if (AreTasksAvailable)
        {
          lock (mTaskQueue)
          {
            foreach (Task task in mTaskQueue)
            {
              task.Function.DynamicInvoke(task.Arguments);
            }

            mTaskQueue.Clear();
          }
        }
#else
		throw new System.NotSupportedException("DispatchTasks is not supported in the Unity Player!");
#endif
      }
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

  public class RiderLogger : ILog
  {
    public bool IsEnabled(LoggingLevel level)
    {
      return level <= RiderPlugin.SelectedLoggingLevel;
    }

    public void Log(LoggingLevel level, string message, Exception exception = null)
    {
      if (!IsEnabled(level))
        return;

      var text = "[Rider][" + level + "]" + DateTime.Now.ToString("HH:mm:ss:ff") + " " + message;

      // using Unity logs causes frequent Unity hangs
      File.AppendAllText(RiderPlugin.logPath,text);
//      switch (level)
//      {
//        case LoggingLevel.FATAL:
//        case LoggingLevel.ERROR:
//          Debug.LogError(text);
//          if (exception != null)
//            Debug.LogException(exception);
//          break;
//        case LoggingLevel.WARN:
//          Debug.LogWarning(text);
//          if (exception != null)
//            Debug.LogException(exception);
//          break;
//        case LoggingLevel.INFO:
//        case LoggingLevel.VERBOSE:
//          Debug.Log(text);
//          if (exception != null)
//            Debug.LogException(exception);
//          break;
//        default:
//          break;
//      }
    }

    public string Category { get; private set; }
  }
}