using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.DataFlow;
using JetBrains.Util.Logging;
using NUnit.Framework.Interfaces;
using UnityEngine.Events;

namespace JetBrains.Rider.Unity.Editor.UnitTesting
{
  public class TestEventsCollector
  {
    public readonly LinkedList<TestEvent> DelayedEvents = new LinkedList<TestEvent>();
    private static string RunnerAddListener = "AddListener";
    private static readonly ILog ourLogger = Log.GetLog(typeof(TestEventsCollector).Name);

    public TestEventsCollector(object playModeTestsController)
    {
      if (playModeTestsController != null)
      {
        AdvisePlayModeTestEvents(playModeTestsController);
      }
    }
    
    private bool AdvisePlayModeTestEvents(object runner)
    {
      if (!AdviseTestStarted(runner, "testStartedEvent", result =>
      {
        TestEventReceived(new TestEvent(EventType.TestStarted, result));
      }))
        return true;

      if (!AdviseTestFinished(runner, "testFinishedEvent", result =>
      {
        TestEventReceived(new TestEvent(EventType.TestFinished, result));
      }))
        return true;

      if (!AdviseSessionFinished(runner, "runFinishedEvent", result =>
      {
        TestEventReceived(new TestEvent(EventType.RunFinished, result));
      }))
        return true;
      return false;
    }
    
    internal static bool AdviseSessionFinished(object runner, string fieldName, Action<ITestResult> callback)
    {
      var mRunFinishedEventMethodInfo= runner.GetType()
        .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

      if (mRunFinishedEventMethodInfo == null)
      {
        ourLogger.Verbose("Could not find m_RunFinishedEvent via reflection");
        return false;
      }

      var mRunFinished = mRunFinishedEventMethodInfo.GetValue(runner);
      var addListenerMethod = mRunFinished.GetType().GetMethod(RunnerAddListener, BindingFlags.Instance | BindingFlags.Public);

      if (addListenerMethod == null)
      {
        ourLogger.Verbose($"Could not find {RunnerAddListener} of mRunFinished via reflection");
        return false;
      }

      //subscribe for tests callbacks
      addListenerMethod.Invoke(mRunFinished, new object[] {new UnityAction<ITestResult>(callback)});
      return true;
    }

    internal static bool AdviseTestStarted(object runner, string fieldName, Action<ITest> callback)
    {
      var mTestStartedEventMethodInfo = runner.GetType()
        .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

      if (mTestStartedEventMethodInfo == null)
      {
        ourLogger.Verbose("Could not find mTestStartedEventMethodInfo via reflection");
        return false;
      }

      var mTestStarted = mTestStartedEventMethodInfo.GetValue(runner);
      var addListenerMethod =
        mTestStarted.GetType().GetMethod(RunnerAddListener, BindingFlags.Instance | BindingFlags.Public);

      if (addListenerMethod == null)
      {
        ourLogger.Verbose($"Could not find {RunnerAddListener} of mTestStarted via reflection");
        return false;
      }

      //subscribe for tests callbacks
      addListenerMethod.Invoke(mTestStarted, new object[] {new UnityAction<ITest>(callback)});
      return true;
    }

    internal static bool AdviseTestFinished(object runner, string fieldName, Action<ITestResult> callback)
    {
      var mTestFinishedEventMethodInfo = runner.GetType()
        .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

      if (mTestFinishedEventMethodInfo == null)
      {
        ourLogger.Verbose("Could not find m_TestFinishedEvent via reflection");
        return false;
      }

      var mTestFinished = mTestFinishedEventMethodInfo.GetValue(runner);
      var addListenerMethod =
        mTestFinished.GetType().GetMethod(RunnerAddListener, BindingFlags.Instance | BindingFlags.Public);

      if (addListenerMethod == null)
      {
        ourLogger.Verbose("Could not find addListenerMethod via reflection");
        return false;
      }

      //subscribe for tests callbacks
      addListenerMethod.Invoke(mTestFinished, new object[] {new UnityAction<ITestResult>(callback)});
      return true;
    }
        
    private void TestEventReceived(TestEvent testEvent)
    {
      ourLogger.Verbose($"TestEventReceived: {testEvent.myType}");
      DelayedEvents.AddLast(testEvent);
      //if (DelayedLogEvents.Count >= myDelayedLogEventsMaxSize)
      //  DelayedLogEvents.RemoveFirst(); // limit max size
      OnAddEvent(new EventArgs());
    }
    
    public event EventHandler AddEvent;

    public void ClearEvent()
    {
      AddEvent = null;
    }

    private void OnAddEvent(EventArgs e)
    {
      AddEvent?.Invoke(this, e);
    }
  }
}