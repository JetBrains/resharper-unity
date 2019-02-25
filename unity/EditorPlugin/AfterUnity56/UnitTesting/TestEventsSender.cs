using System;
using System.Linq;
using System.Text;
using JetBrains.Diagnostics;
using JetBrains.Platform.Unity.EditorPluginModel;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using TestResult = JetBrains.Platform.Unity.EditorPluginModel.TestResult;

namespace JetBrains.Rider.Unity.Editor.AfterUnity56.UnitTesting
{
  public class TestEventsSender
  {
    private readonly UnitTestLaunch myUnitTestLaunch;
    private static readonly ILog ourLogger = Log.GetLog(typeof(TestEventsSender).Name);

    internal TestEventsSender(TestEventsCollector collector, UnitTestLaunch unitTestLaunch)
    {
      myUnitTestLaunch = unitTestLaunch;
      ProcessQueue(collector);
      collector.Clear();

      collector.ClearEvent();
      collector.AddEvent += (col, _) =>
      {
        ProcessQueue((TestEventsCollector)col);
      };
    }
    
    private void ProcessQueue(TestEventsCollector collector)
    {
      if (!collector.DelayedEvents.Any())
        return;

      foreach (var myEvent in collector.DelayedEvents)
      {
        switch (myEvent.myType)
        {
          case EventType.RunFinished:
            RunFinished(myUnitTestLaunch);
            break;
          case EventType.TestFinished:
            TestFinished(myUnitTestLaunch, GetTestResult(myEvent.Event));
            break;
          case EventType.TestStarted:
            var tResult = new TestResult(myEvent.Event.myID, myEvent.Event.myAssemblyName,string.Empty, 0, Status.Running, myEvent.Event.myParentID);
            TestStarted(myUnitTestLaunch, tResult);
            break;
        }        
      }

      collector.Clear();
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

    internal static TestResult GetTestResult(TestInternalEvent tEvent)
    {
      return new TestResult(tEvent.myID, tEvent.myAssemblyName, tEvent.myOutput, tEvent.myDuration, tEvent.myStatus, tEvent.myParentID);
    }

    internal static TestInternalEvent GetTestResult(ITestResult testResult)
    {
      //ourLogger.Verbose("TestFinished : {0}, result : {1}", test.FullName, testResult.ResultState);
      var id = GetIdFromNUnitTest(testResult.Test);
      var assemblyName = testResult.Test.TypeInfo.Assembly.GetName().Name;

      var output = ExtractOutput(testResult);
      Status status;
      if (Equals(testResult.ResultState, ResultState.Success))
        status = Status.Success;
      else if (Equals(testResult.ResultState, ResultState.Ignored))
        status = Status.Ignored;
      else if (Equals(testResult.ResultState, ResultState.Inconclusive) ||
               Equals(testResult.ResultState, ResultState.Skipped))
        status = Status.Inconclusive;
      else
        status = Status.Failure;
      return new TestInternalEvent(id, assemblyName, output,
        (int) TimeSpan.FromMilliseconds(testResult.Duration).TotalMilliseconds,
        status, GetIdFromNUnitTest(testResult.Test.Parent));
    }

    internal static string ExtractOutput(ITestResult testResult)
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
        return test.FullName;
      }

      return test.FullName;
    }
  }
}