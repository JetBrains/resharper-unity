using System;
using System.Collections.Generic;
using JetBrains.Platform.RdFramework;
using UnityEditor;

namespace Plugins.Editor.JetBrains
{
  public class MainThreadDispatcher : IScheduler
  {
    public MainThreadDispatcher()
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
      if (IsActive)
      {
        lock (myTaskQueue)
        {
          foreach (Action task in myTaskQueue)
          {
            task();
          }

          myTaskQueue.Clear();
        }
      }
    }

    /// <summary>
    /// Indicates whether there are tasks available for dispatching
    /// </summary>
    /// <value>
    /// <c>true</c> if there are tasks available for dispatching; otherwise, <c>false</c>.
    /// </value>
    public bool IsActive
    {
      get { return myTaskQueue.Count > 0; }
    }
    public bool OutOfOrderExecution
    {
      get { return false; }
    }
  }
}