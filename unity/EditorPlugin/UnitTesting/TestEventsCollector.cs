using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Util.Logging;
using NUnit.Framework.Interfaces;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace JetBrains.Rider.Unity.Editor.UnitTesting
{
  [Serializable]
  public class TestEventsCollector : ScriptableObject
  {
    [SerializeField]
    public List<TestEvent> DelayedEvents = new List<TestEvent>();
    
    private static readonly ILog ourLogger = Log.GetLog("TestEventsCollector");
    private Component Controller;
   
    public void SetupPersistentListeners(object runner)
    {
      UnityEventTools.AddPersistentListener((UnityEvent<ITest>) runner.GetType().GetField("testStartedEvent",
          BindingFlags.NonPublic | BindingFlags.Instance).GetValue(runner),
        result => TestEventReceived(new TestEvent(EventType.TestStarted, result)));
      UnityEventTools.AddPersistentListener((UnityEvent<ITestResult>) runner.GetType().GetField("testFinishedEvent",
          BindingFlags.NonPublic | BindingFlags.Instance).GetValue(runner),
        result => TestEventReceived(new TestEvent(EventType.TestFinished, result)));
      UnityEventTools.AddPersistentListener((UnityEvent<ITestResult>) runner.GetType().GetField("runFinishedEvent",
          BindingFlags.NonPublic | BindingFlags.Instance).GetValue(runner),
        result => TestEventReceived(new TestEvent(EventType.RunFinished, result)));
    }
        
    public void TestEventReceived(TestEvent testEvent)
    {
      ourLogger.Verbose($"TestEventReceived: {testEvent.myType}");
      DelayedEvents.Add(testEvent);
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