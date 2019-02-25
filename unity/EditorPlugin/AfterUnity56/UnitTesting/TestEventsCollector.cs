using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Diagnostics;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.Rider.Unity.Editor.Utils;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

// removing "redundant" new UnityAction<ITestResult> causes events not fire in PlayMode tests in Unity 2017.x
// ReSharper disable RedundantDelegateCreation

namespace JetBrains.Rider.Unity.Editor.AfterUnity56.UnitTesting
{
  [Location("JetBrainsRiderTestEventsCollectorCache.txt", LocationAttribute.Location.LibraryFolder)]
  internal class TestEventsCollector : ScriptObjectSingleton<TestEventsCollector>
  {
    private void OnEnable()
    {
      // required for Unity 2017.x
      hideFlags = HideFlags.DontUnloadUnusedAsset;
    }

    public List<TestEvent> DelayedEvents = new List<TestEvent>();

    private static readonly ILog ourLogger = Log.GetLog("TestEventsCollector");
    
    public void SetupPersistentListeners(object runner)
    {
      UnityEventTools.AddPersistentListener((UnityEvent<ITest>) runner.GetType().GetField("testStartedEvent",
          BindingFlags.NonPublic | BindingFlags.Instance).GetValue(runner), new UnityAction<ITest>(test =>
        {
          if (!(test is TestMethod)) return;
          ourLogger.Verbose("TestStarted : {0}", test.FullName);
          var internalEvent = new TestInternalEvent(TestEventsSender.GetIdFromNUnitTest(test), test.Method.TypeInfo.Assembly.GetName().Name, "", 0, Status.Running, TestEventsSender.GetIdFromNUnitTest(test.Parent));
          TestEventReceived(new TestEvent(EventType.TestStarted, internalEvent));
        })
        );
      UnityEventTools.AddPersistentListener((UnityEvent<ITestResult>) runner.GetType().GetField("testFinishedEvent",
          BindingFlags.NonPublic | BindingFlags.Instance).GetValue(runner), new UnityAction<ITestResult>(result =>
        {
          if (!(result.Test is TestMethod)) return;
          
          var internalEvent = TestEventsSender.GetTestResult(result);
          TestEventReceived(new TestEvent(EventType.TestFinished, internalEvent));
        }));
      UnityEventTools.AddPersistentListener((UnityEvent<ITestResult>) runner.GetType().GetField("runFinishedEvent",
          BindingFlags.NonPublic | BindingFlags.Instance).GetValue(runner),
        new UnityAction<ITestResult>(result =>
        {
          var internalEvent = new TestInternalEvent("", "","", 0, Status.Success, ""); 
          TestEventReceived(new TestEvent(EventType.RunFinished, internalEvent));
        }));
    }

    public void Clear()
    {
      DelayedEvents.Clear();
      Save(true);
    }
        
    public void TestEventReceived(TestEvent testEvent)
    {
      ourLogger.Verbose($"TestEventReceived: {testEvent.myType}");
      DelayedEvents.Add(testEvent);
      Save(true); // todo: save once on changing playmode
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