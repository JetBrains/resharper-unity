using System;
using System.Diagnostics;
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

namespace Plugins.Editor.JetBrains
{
  [InitializeOnLoad]
  public static class RiderProtocolController
  {
    static RiderProtocolController()
    {
      Start(Path.Combine(Path.GetTempPath(), "Unity3dRider",
        "Unity3dRider" + DateTime.Now.ToString("YYYY-MM-ddT-HH-mm-ss") + ".log"));
    }

    private static void Start(string loggerPath)
    {
      var lifetimeDefinition = Lifetimes.Define(EternalLifetime.Instance);
      var lifetime = lifetimeDefinition.Lifetime;

      ILogger myLogger = Logger.GetLogger("Core");
      var fileLogEventListener = new FileLogEventListener(loggerPath, false); // works in Unity mono 4.6
      //var fileLogEventListener = new FileLogEventListener(loggerPath); //fails in Unity mono 4.6
      LogManager.Instance.AddOmnipresentLogger(lifetime, fileLogEventListener, LoggingLevel.VERBOSE);

      try
      {
        myLogger.Info("Unity process with Id = " + Process.GetCurrentProcess().Id);
        myLogger.Info("Start ControllerTask...");

        int port = 46000 + Process.GetCurrentProcess().Id % 1000;

        var dispatcher = new RdSimpleDispatcher(lifetime, myLogger);

        myLogger.Info("Create protocol...");
        var protocol = new Protocol(new Serializers(), new Identities(IdKind.DynamicClient), dispatcher,
          creatingProtocol =>
          {
            myLogger.Info("Creating SocketWire with port = {0}", port);
            return new SocketWire.Server(lifetime, creatingProtocol, port, "UnityServer");
          });

        myLogger.Info("Create UnityModel and advise for new sessions...");
        var model = new UnityModel(lifetime, protocol);
        model.Play.Advise(lifetime, session =>
        {
          myLogger.Info("model.Play.Advise: " + session);
          if (!session) return;
          var text = "Edit/Play";
          UnityDispatcher.Dispatch(() =>
          {
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

    /// <summary>
    /// Editor Thread Dispatcher
    /// Provides a means to execute a function on a Unity owned thread
    /// </summary>
    [InitializeOnLoad]
    private class UnityDispatcher
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
      static UnityDispatcher()
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
      public static void Dispatch(Action function)
      {
        Dispatch(function, null);
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
      private static void Dispatch(Delegate function, params object[] arguments)
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
      /// Clears the queued tasks
      /// </summary>
      /// <exception cref='System.NotSupportedException'>
      /// Is thrown when this method is called from the Unity Player
      /// </exception>
      public static void ClearTasks()
      {
#if UNITY_EDITOR
        if (AreTasksAvailable)
        {
          lock (mTaskQueue)
          {
            mTaskQueue.Clear();
          }
        }
#else
		throw new System.NotSupportedException("ClearTasks is not supported in the Unity Player!");
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