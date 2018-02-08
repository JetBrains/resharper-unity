using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Platform.RdFramework;
using JetBrains.Util;
using JetBrains.Util.Logging;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor
{
  public class MainThreadDispatcher : IScheduler
  {
    internal static readonly MainThreadDispatcher Instance = new MainThreadDispatcher();

    private static Thread ourUIThread;

    private MainThreadDispatcher()
    {
      EditorApplication.update += DispatchTasks;
    }

    /// <summary>
    /// The queue of tasks that are being requested for the next time DispatchTasks is called
    /// </summary>
    private readonly Queue<Action> myTaskQueue = new Queue<Action>();

    /// <summary>
    /// Dispatches the specified action delegate.
    /// </summary>
    /// <param name="action">Action  being requested</param>
    public void Queue(Action action)
    {
      lock (myTaskQueue)
      {
        myTaskQueue.Enqueue(action);
      }
    }

    /// <summary>
    /// Dispatches the tasks that has been requested since the last call to this function
    /// </summary>
    private void DispatchTasks()
    {
//        File.AppendAllText(logPath, DateTime.Now.ToString(global::JetBrains.Util.Logging.Log.DefaultDateFormat) + "DispatchTasks"+Environment.NewLine);
      //RiderPlugin.Log(LoggingLevel.INFO, "DispatchTasks");

      ourUIThread = Thread.CurrentThread;
      
      if (myTaskQueue.Count == 0)
        return;
      while (true)
      {
        try
        {
          if (myTaskQueue.Count == 0)
            return;
          var task = myTaskQueue.Dequeue();
          task();
        }
        catch (Exception e)
        {
          Log.GetLog<MainThreadDispatcher>().Error(e);
        }
      }

    }

    public static void AssertThread()
    {
      Assertion.Require(ourUIThread == null || ourUIThread == Thread.CurrentThread, "Not not UI thread");
    }
    
    /// <summary>
    /// Indicates whether there are tasks available for dispatching
    /// </summary>
    /// <value>
    /// <c>true</c> if there are tasks available for dispatching; otherwise, <c>false</c>.
    /// </value>
    public bool IsActive => ourUIThread == null || ourUIThread == Thread.CurrentThread;

    public bool OutOfOrderExecution => false;
  }
}