using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor
{
  public class MainThreadDispatcher : IScheduler
  {
    private static Thread ourUIThread;
    internal static readonly MainThreadDispatcher Instance = new MainThreadDispatcher();
    /// <summary>
    /// The queue of tasks that are being requested for the next time DispatchTasks is called
    /// </summary>
    private readonly Queue<Action> myTaskQueue = new Queue<Action>();

    private MainThreadDispatcher()
    {
      EditorApplication.update += DispatchTasks;
    }
    
    /// <summary>
    /// Dispatches the specified action delegate.
    /// </summary>
    /// <param name="action">Action  being requested</param>
    public void Queue(Action action)
    {
      if (Thread.CurrentThread == ourUIThread)
      {
        action();
        return;
      }
      
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
      Assertion.Require(ourUIThread == null || ourUIThread == Thread.CurrentThread, "Not a UI thread");
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