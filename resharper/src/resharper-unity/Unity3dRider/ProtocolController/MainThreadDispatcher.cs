using System;
using System.Collections.Generic;
using UnityEditor;

namespace Plugins.Editor.JetBrains
{
  [InitializeOnLoad]
  static class MainThreadDispatcher
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
      if (!RiderPlugin1.Enabled)
        return;
        
      EditorApplication.update += DispatchTasks;
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
      lock (mTaskQueue)
      {
        mTaskQueue.Enqueue(new Task(function, arguments));
      }
    }

    /// <summary>
    /// Dispatches the tasks that has been requested since the last call to this function
    /// </summary>
    /// <exception cref='System.NotSupportedException'>
    /// Is thrown when this method is called from the Unity Player
    /// </exception>
    private static void DispatchTasks()
    {
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
    }
  }
}