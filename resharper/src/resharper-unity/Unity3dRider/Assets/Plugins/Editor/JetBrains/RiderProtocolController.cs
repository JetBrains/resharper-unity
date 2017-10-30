﻿#if NET_4_6

using System;
using System.IO;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.Platform.RdFramework.Impl;
using JetBrains.Platform.Unity.Model;
using JetBrains.Util;
using JetBrains.Util.Logging;
using UnityEditor;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using ILogger = JetBrains.Util.ILogger;
using Logger = JetBrains.Util.Logging.Logger;

namespace Plugins.Editor.JetBrains
{
  [InitializeOnLoad]
  public static class RiderProtocolController
  {
    static RiderProtocolController()
    {
      var projectDirectory = Directory.GetParent(Application.dataPath).FullName;
      var logPath = Path.Combine(Path.GetTempPath(), "Unity3dRider",
        "Unity3dRider" + DateTime.Now.ToString("YYYY-MM-ddT-HH-mm-ss") + ".log");
      var myThread = new Thread(() => Start(projectDirectory, logPath));
      myThread.Start();
    }

    private static void Start(string projectDirectory, string loggerPath)
    {
      var lifetimeDefinition = Lifetimes.Define(EternalLifetime.Instance);
      var lifetime = lifetimeDefinition.Lifetime;

      ILogger myLogger = Logger.GetLogger("Core");
      var fileLogEventListener = new FileLogEventListener(loggerPath, false); // works in Unity mono 4.6
      //var fileLogEventListener = new FileLogEventListener(loggerPath); //fails in Unity mono 4.6
      LogManager.Instance.AddOmnipresentLogger(lifetime, fileLogEventListener, LoggingLevel.TRACE);
//      LogManager.Instance.ApplyTransformation(lifetime, config =>
//      {
//        config.InjectNode(new LogConfNode(LoggingLevel.TRACE, "protocol"));
//      });

      try
      {
        myLogger.Info("Start ControllerTask...");

        var dispatcher = new SimpleInpaceExecutingScheduler(myLogger);
        
        myLogger.Info("Create protocol...");
        var protocol = new Protocol(new Serializers(), new Identities(IdKind.DynamicServer), dispatcher,
          creatingProtocol =>
          {
            var wire = new SocketWire.Server(lifetime, creatingProtocol, null, "UnityServer");
            myLogger.Info("Creating SocketWire with port = {0}", wire.Port);
            
            InitializeProtocolJson(wire.Port, projectDirectory, myLogger);
            return wire;
          });

        myLogger.Info("Create UnityModel and advise for new sessions...");
        var model = new UnityModel(lifetime, protocol);
        model.Play.Advise(lifetime, play =>
        {
          myLogger.Info("model.Play.Advise: " + play);
          var text = "Edit/Play";
          MainThreadDispatcher.Queue(() =>
          {
            if (!Application.isPlaying && play || Application.isPlaying && !play)
              EditorApplication.ExecuteMenuItem(text);
          });
        });
        model.HostConnected.SetValue(true);

        //myLogger.Info("Run dispatcher...");
        //dispatcher.Run(); // Unity already has dispatcher
      }
      catch (Exception ex)
      {
        myLogger.Error(ex);
      }
    }

    private static void InitializeProtocolJson(int port, string projectDirectory, ILogger logger)
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

    [InitializeOnLoad]
    private static class MainThreadDispatcher
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
}
#endif