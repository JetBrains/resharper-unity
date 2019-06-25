using System;
using System.Linq;
using System.Text;
using JetBrains.Diagnostics;
using JetBrains.Platform.Unity.EditorPluginModel;
using Newtonsoft.Json;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEngine;
using TestResult = JetBrains.Platform.Unity.EditorPluginModel.TestResult;

namespace JetBrains.Rider.Unity.Editor.AfterUnity56.UnitTesting
{
  public class TestEventsSender
  {
    private readonly UnitTestLaunch myUnitTestLaunch;
    private static readonly ILog ourLogger = Log.GetLog(typeof(TestEventsSender).Name);
    
    internal TestEventsSender(UnitTestLaunch unitTestLaunch)
    {
      myUnitTestLaunch = unitTestLaunch;
      
      var myAssembly = RiderPackageInterop.GetAssembly();
      if (myAssembly == null) return;
      var myData = myAssembly.GetType("Packages.Rider.Editor.UnitTesting.CallbackData");
      if (myData == null) return;

      ProcessQueue(myData, myUnitTestLaunch);

      SubscribeToChanged(myData, myUnitTestLaunch);
    }

    private static void SubscribeToChanged(Type data, UnitTestLaunch myUnitTestLaunch)
    {
      var eventInfo = data.GetEvent("Changed");
      
      if (eventInfo != null)
      {
        var handler = new EventHandler((sender, e) => { ProcessQueue(data, myUnitTestLaunch); });
        eventInfo.AddEventHandler(handler.Target, handler);
        AppDomain.CurrentDomain.DomainUnload += (EventHandler) ((_, __) =>
        {
          eventInfo.RemoveEventHandler(handler.Target, handler);
        });
      }
      else
      {
        ourLogger.Error("Changed event subscription failed.");
      }
    }
    
    private static void ProcessQueue(Type data, UnitTestLaunch unitTestLaunch)
    {
      var baseType = data.BaseType;
      if (baseType == null) return;
      var instance = baseType.GetProperty("instance");
      if (instance == null) return;
      var myInstanceVal = instance.GetValue(null, new object[]{});
      var myGetJsonAndClearMethod = data.GetMethod("GetJsonAndClear");
      if (myGetJsonAndClearMethod == null) return;
      var json = (string)myGetJsonAndClearMethod.Invoke(myInstanceVal, new object[] {});
      Debug.Log(json);
      var delayedEventsObject = JsonConvert.DeserializeObject<DelayedEvents>(json);
      Debug.Log(delayedEventsObject);
      var events = delayedEventsObject.events;
      Debug.Log(events.Length);
      if (!events.Any())
        return;

      foreach (var myEvent in events)
      {
        switch (myEvent.type)
        {
          case EventType.RunFinished:
            RunFinished(unitTestLaunch);
            break;
          case EventType.TestFinished:
            TestFinished(unitTestLaunch, GetTestResult(myEvent));
            break;
          case EventType.TestStarted:
            var tResult = new TestResult(myEvent.id, myEvent.assemblyName,string.Empty, 0, Status.Running, myEvent.parentID);
            TestStarted(unitTestLaunch, tResult);
            break;
        }        
      }
    }
    
    public static void RunFinished(UnitTestLaunch launch)
    {
      launch.RunResult(new RunResult(true));
    }
    
    public static void TestStarted(UnitTestLaunch launch, TestResult testResult)
    {
      launch.TestResult(testResult);
    }

    public static void TestFinished(UnitTestLaunch launch, TestResult testResult)
    {
      launch.TestResult(testResult);
    }

    internal static TestResult GetTestResult(TestEvent tEvent)
    {
      var status = GetStatus(new ResultState((TestStatus)Enum.Parse(typeof(TestStatus), tEvent.resultState)));
      
      return new TestResult(tEvent.id, tEvent.assemblyName, tEvent.output, (int) TimeSpan.FromMilliseconds(tEvent.duration).TotalMilliseconds, 
        status, tEvent.parentID);
    }

    internal static TestResult GetTestResult(ITestResult testResult)
    {
      var id = GetIdFromNUnitTest(testResult.Test);
      var assemblyName = testResult.Test.TypeInfo.Assembly.GetName().Name;

      var output = ExtractOutput(testResult);
      var status = GetStatus(testResult.ResultState);
      return new TestResult( id, assemblyName, output,
        (int) TimeSpan.FromMilliseconds(testResult.Duration).TotalMilliseconds,
        status, GetIdFromNUnitTest(testResult.Test.Parent));
    }

    private static Status GetStatus(ResultState resultState)
    {
      Status status;
      if (Equals(resultState, ResultState.Success))
        status = Status.Success;
      else if (Equals(resultState, ResultState.Ignored))
        status = Status.Ignored;
      else if (Equals(resultState, ResultState.Inconclusive) ||
               Equals(resultState, ResultState.Skipped))
        status = Status.Inconclusive;
      else
        status = Status.Failure;
      return status;
    }

    private static string ExtractOutput(ITestResult testResult)
    {
      var stringBuilder = new StringBuilder();
      if (testResult.Message != null)
      {
        stringBuilder.AppendLine("Message: ");
        stringBuilder.AppendLine(testResult.Message);
      }

      if (!string.IsNullOrEmpty(testResult.Output))
      {
        stringBuilder.AppendLine("Output: ");
        stringBuilder.AppendLine(testResult.Output);
      }

      if (!string.IsNullOrEmpty(testResult.StackTrace))
      {
        stringBuilder.AppendLine("Stacktrace: ");
        stringBuilder.AppendLine(testResult.StackTrace);
      }
      
      var result = stringBuilder.ToString();
      if (result.Length > 0)
        return result;

      return testResult.Output ?? String.Empty;
    }

    internal static string GetIdFromNUnitTest(ITest test)
    {
      var testMethod = test as TestMethod;
      if (testMethod == null)
      {
        ourLogger.Verbose("{0} is not a TestMethod ", test.FullName);
        return GetUniqueName(test);
      }

      return GetUniqueName(test);
    }

    // analog of UnityEngine.TestRunner.NUnitExtensions.TestExtensions.GetUniqueName
    // I believe newer nunit has improved parameters presentation compared to the one used in Unity.
    // https://github.com/nunit/nunit/blob/d56424858f97e19a5fe64905e42adf798ca655d1/src/NUnitFramework/framework/Internal/TestNameGenerator.cs#L223
    // so once Unity updates its nunit, this hack would not be needed anymore
    private static string GetUniqueName(ITest test)
    {
      string str = test.FullName;
      if (HasChildIndex(test))
      {
        int childIndex = GetChildIndex(test);
        if (childIndex >= 0)
          str += childIndex;
      }
      
      return str;
    }

    private static int GetChildIndex(ITest test)
    {
      return (int) test.Properties["childIndex"][0];
    }

    private static bool HasChildIndex(ITest test)
    {
      return test.Properties["childIndex"].Count > 0;
    }
  }
}