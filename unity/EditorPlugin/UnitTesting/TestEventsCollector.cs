using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.Rider.Unity.Editor.Utils;
using JetBrains.Util.Logging;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

namespace JetBrains.Rider.Unity.Editor.UnitTesting
{
  [Location("JetBrainsRiderTestEventsCollectorCache.txt", LocationAttribute.Location.LibraryFolder)]
  internal class TestEventsCollector : ScriptObjectSingleton<TestEventsCollector>
  {
    public List<TestEvent> DelayedEvents = new List<TestEvent>();

    private static readonly ILog ourLogger = Log.GetLog("TestEventsCollector");
    private Component Controller;
   
    public void SetupPersistentListeners(object runner)
    {
      UnityEventTools.AddPersistentListener((UnityEvent<ITest>) runner.GetType().GetField("testStartedEvent",
          BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(runner),
        test =>
        {
          if (!(test is TestMethod)) return;
          ourLogger.Verbose("TestStarted : {0}", test.FullName);
          var internalEvent = new TestInternalEvent(TestEventsSender.GetIdFromNUnitTest(test), "", 0, Status.Running, TestEventsSender.GetIdFromNUnitTest(test.Parent));
          TestEventReceived(new TestEvent(EventType.TestStarted, internalEvent));
        });
      UnityEventTools.AddPersistentListener((UnityEvent<ITestResult>) runner.GetType().GetField("testFinishedEvent",
          BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(runner),
        result =>
        {
          if (!(result.Test is TestMethod)) return;
          
          var internalEvent = TestEventsSender.GetTestResult(result);
          TestEventReceived(new TestEvent(EventType.TestFinished, internalEvent));
        });
      UnityEventTools.AddPersistentListener((UnityEvent<ITestResult>) runner.GetType().GetField("runFinishedEvent",
          BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(runner),
        result =>
        {
          var internalEvent = new TestInternalEvent("", "", 0, Status.Success, ""); 
          TestEventReceived(new TestEvent(EventType.RunFinished, internalEvent));
        });
    }
        
    public void TestEventReceived(TestEvent testEvent)
    {
      ourLogger.Verbose($"TestEventReceived: {testEvent.myType}");
      DelayedEvents.Add(testEvent);
      Save(true);
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