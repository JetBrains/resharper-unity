using System;
using System.Collections;
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
    private static readonly ILog ourLogger = Log.GetLog(typeof(TestEventsSender).Name);
    
    internal TestEventsSender(UnitTestLaunch unitTestLaunch)
    { 
      var assembly = RiderPackageInterop.GetAssembly();
      if (assembly == null)
      {
        ourLogger.Error("EditorPlugin assembly is null.");
        return;
      }

      var data = assembly.GetType("Packages.Rider.Editor.UnitTesting.CallbackData");
      if (data == null) return;

      ProcessQueue(data, unitTestLaunch);

      SubscribeToChanged(data, unitTestLaunch);
    }

    private static void SubscribeToChanged(Type data, UnitTestLaunch unitTestLaunch)
    {
      var eventInfo = data.GetEvent("Changed");
      
      if (eventInfo != null)
      {
        var handler = new EventHandler((sender, e) => { ProcessQueue(data, unitTestLaunch); });
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
      if (!unitTestLaunch.IsBound)
        return;
      
      var baseType = data.BaseType;
      if (baseType == null) return;
      var instance = baseType.GetProperty("instance");
      if (instance == null) return;
      var instanceVal = instance.GetValue(null, new object[]{});

      var listField = data.GetField("events");
      if (listField == null) return;
      var list = listField.GetValue(instanceVal);

      var events = (IEnumerable) list;
      
      foreach (var ev in events)
      {
        var type = (int)ev.GetType().GetField("type").GetValue(ev);
        var id = (string)ev.GetType().GetField("id").GetValue(ev);
        var assemblyName = (string)ev.GetType().GetField("assemblyName").GetValue(ev);
        var output = (string)ev.GetType().GetField("output").GetValue(ev);
        var resultState = (int)ev.GetType().GetField("testStatus").GetValue(ev);
        var duration = (double)ev.GetType().GetField("duration").GetValue(ev);
        var parentId = (string)ev.GetType().GetField("parentId").GetValue(ev);
        
        switch (type)
        {
          case 0: // TestStarted
          {
            var tResult = new TestResult(id, assemblyName,string.Empty, 0, Status.Running, parentId);
            TestStarted(unitTestLaunch, tResult);
            break;
          }
          case 1: // TestFinished
          {
            var status = GetStatus(new ResultState((TestStatus)resultState));

            var testResult = new TestResult(id, assemblyName, output, (int) TimeSpan.FromMilliseconds(duration).TotalMilliseconds, 
              status, parentId);
            TestFinished(unitTestLaunch, testResult);
            break;
          }
          case 2: // RunFinished
          {
            var runResult = new RunResult((TestStatus) resultState == TestStatus.Passed);
            RunFinished(unitTestLaunch, runResult);
            break;
          }
          case 3: // RunStarted
          {
            unitTestLaunch.RunStarted.Value = true;
            break;
          }
          default:
          {
            ourLogger.Error("Unexpected TestEvent type.");
            break;
          }
        }
      }
      
      var clearMethod = data.GetMethod("Clear");
      clearMethod?.Invoke(instanceVal, new object[] {});
    }

    public static void RunFinished(UnitTestLaunch launch, RunResult result)
    {
      launch.RunResult(result);
    }
    
    public static void TestStarted(UnitTestLaunch launch, TestResult testResult)
    {
      launch.TestResult(testResult);
    }

    public static void TestFinished(UnitTestLaunch launch, TestResult testResult)
    {
      launch.TestResult(testResult);
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