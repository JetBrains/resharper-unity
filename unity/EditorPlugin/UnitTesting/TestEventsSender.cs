using System;
using System.Linq;
using System.Text;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.Util.Logging;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using TestResult = JetBrains.Platform.Unity.EditorPluginModel.TestResult;

namespace JetBrains.Rider.Unity.Editor.UnitTesting
{
  public class TestEventsSender
  {
    private readonly UnitTestLaunch myUnitTestLaunch;
    private static readonly ILog ourLogger = Log.GetLog(typeof(TestEventsSender).Name);

    public TestEventsSender(TestEventsCollector collector, UnitTestLaunch unitTestLaunch)
    {
      myUnitTestLaunch = unitTestLaunch;
      ProcessQueue(collector);
      collector.DelayedEvents.Clear();

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

      var head = collector.DelayedEvents.First;
      while (head != null)
      {
        var res = head.Value;
        switch (res.myType)
        {
          case EventType.RunFinished:
            RunFinished(myUnitTestLaunch, (ITestResult) res.myTestEvent);
            break;
          case EventType.TestFinished:
            TestFinished(myUnitTestLaunch, (ITestResult) res.myTestEvent);
            break;
          case EventType.TestStarted:
            TestStarted(myUnitTestLaunch, (ITest) res.myTestEvent);
            break;
        }

        head = head.Next;
      }
      collector.DelayedEvents.Clear();
    }
    
    public static void RunFinished(UnitTestLaunch launch, ITestResult test)
    {
      launch.RunResult.Fire(new RunResult(true));
    }
    
    public static void TestStarted(UnitTestLaunch launch, ITest test)
    {
      if (!(test is TestMethod))
        return;

      ourLogger.Verbose("TestStarted : {0}", test.FullName);
      var id = GetIdFromNUnitTest(test);

      launch.TestResult.Fire(new TestResult(id, string.Empty, 0, Status.Running, GetIdFromNUnitTest(test.Parent)));
    }

    public static void TestFinished(UnitTestLaunch launch, ITestResult testResult)
    {
      var test = testResult.Test;
      if (!(test is TestMethod))
        return;

      ourLogger.Verbose("TestFinished : {0}, result : {1}", test.FullName, testResult.ResultState);
      var id = GetIdFromNUnitTest(test);

      var output = ExtractOutput(testResult);
      Status status;
      if (Equals(testResult.ResultState, ResultState.Success))
        status = Status.Success;
      else if (Equals(testResult.ResultState, ResultState.Ignored))
        status = Status.Ignored;
      else if (Equals(testResult.ResultState, ResultState.Inconclusive) || Equals(testResult.ResultState, ResultState.Skipped))
        status = Status.Inconclusive;
      else
        status = Status.Failure;
          
      launch.TestResult.Fire(new TestResult(id, output,
        (int) TimeSpan.FromMilliseconds(testResult.Duration).TotalMilliseconds,
        status, GetIdFromNUnitTest(test.Parent)));
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

    private static string GetIdFromNUnitTest(ITest test)
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