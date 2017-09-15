using System;
using System.Collections.Generic;
using UnityEditor;

namespace Plugins.Editor.JetBrains
{
  /// <summary>
  /// Editor Thread Dispatcher
  /// Provides a means to execute a function on a Unity owned thread
  /// </summary>
  [InitializeOnLoad]
  class Dispatcher
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
    static Dispatcher()
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
    public static void Dispatch(Delegate function, params object[] arguments)
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