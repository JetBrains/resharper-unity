using System;
using System.Reflection;
using System.Text;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.Util.Logging;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEngine.Events;
using TestResult = JetBrains.Platform.Unity.EditorPluginModel.TestResult;

namespace JetBrains.Rider.Unity.Editor.UnitTesting
{
  public class TestListenersStarter
  {
    private static readonly ILog ourLogger = Log.GetLog(typeof(TestListenersStarter).Name);
    private static string RunnerAddListener = "AddListener";    
    private static UnitTestLaunch ourLaunch;

    public TestListenersStarter(UnitTestLaunch launch, object playModeTestsController)
    {
      if (playModeTestsController != null)
        SubscribePlayModeListeners(playModeTestsController);
      ourLaunch = launch;
    }
    
    private static bool SubscribePlayModeListeners(object runner)
    {
      if (!AdviseTestStarted(runner, "testStartedEvent"))
        return true;

      if (!AdviseTestFinished(runner, "testFinishedEvent"))
        return true;

      if (!AdviseSessionFinished(runner, "runFinishedEvent"))
        return true;
      return false;
    }

    
    internal static bool AdviseSessionFinished(object runner, string fieldName)
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
      addListenerMethod.Invoke(mRunFinished, new object[] {new UnityAction<ITestResult>(RunFinished)});
      return true;
    }

    internal static bool AdviseTestStarted(object runner, string fieldName)
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
      addListenerMethod.Invoke(mTestStarted, new object[] {new UnityAction<ITest>(TestStarted)});
      return true;
    }

    internal static bool AdviseTestFinished(object runner, string fieldName)
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
      addListenerMethod.Invoke(mTestFinished, new object[] {new UnityAction<ITestResult>(TestFinished)});
      return true;
    }
    
    private static void RunFinished(ITestResult test)
    {
      ourLaunch.RunResult.Fire(new RunResult(true));
    }

    private static void TestStarted(ITest test)
    {
      if (!(test is TestMethod))
        return;

      ourLogger.Verbose("TestStarted : {0}", test.FullName);
      var id = GetIdFromNUnitTest(test);

      ourLaunch.TestResult.Fire(new TestResult(id, string.Empty, 0, Status.Running, GetIdFromNUnitTest(test.Parent)));
    }

    private static void TestFinished(ITestResult testResult)
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
          
      ourLaunch.TestResult.Fire(new TestResult(id, output,
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