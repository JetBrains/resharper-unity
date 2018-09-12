using System;
using System.Linq;
using System.Reflection;
using System.Text;
using JetBrains.Platform.RdFramework.Tasks;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.Util;
using JetBrains.Util.Logging;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEngine.Events;
using TestResult = JetBrains.Platform.Unity.EditorPluginModel.TestResult;

namespace JetBrains.Rider.Unity.Editor.UnitTesting
{
  public class UnityEditorTestLauncher
  {
    private readonly UnitTestLaunch myLaunch;
    private const string RunnerAddListener = "AddListener";
    private static readonly ILog ourLogger = Log.GetLog("RiderPlugin");

    public UnityEditorTestLauncher(UnitTestLaunch launch)
    {
      myLaunch = launch;
    }

    public void TryLaunchUnitTests()
    {
      try
      {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var testEditorAssembly = assemblies
          .FirstOrDefault(assembly => assembly.GetName().Name.Equals("UnityEditor.TestRunner"));
        var testEngineAssembly = assemblies
          .FirstOrDefault(assembly => assembly.GetName().Name.Equals("UnityEngine.TestRunner"));

        if (testEditorAssembly == null || testEngineAssembly == null)
        {
          ourLogger.Verbose(
            "Could not find UnityEditor.TestRunner or UnityEngine.TestRunner assemblies in current AppDomain");
          return;
        }
        
        string testEditorAssemblyProperties =  testEditorAssembly.GetType().GetProperties().Select(a=>a.Name).Aggregate((a, b)=>a+ ", "+b);
        string testEngineAssemblyProperties = testEngineAssembly.GetType().GetProperties().Select(a=>a.Name).Aggregate((a, b)=>a+ ", "+b);

        var launcherTypeString = myLaunch.TestMode == TestMode.Edit ? 
          "UnityEditor.TestTools.TestRunner.EditModeLauncher" : 
          "UnityEditor.TestTools.TestRunner.PlaymodeLauncher";
        var launcherType = testEditorAssembly.GetType(launcherTypeString);
        if (launcherType == null)
          throw new NullReferenceException($"Could not find {launcherTypeString} among {testEditorAssemblyProperties}");
        
        var filterType = testEngineAssembly.GetType("UnityEngine.TestTools.TestRunner.GUI.TestRunnerFilter");
        if (filterType==null)
          throw new NullReferenceException($"Could not find \"UnityEngine.TestTools.TestRunner.GUI.TestRunnerFilter\" among {testEngineAssemblyProperties}");
        
        var filter = Activator.CreateInstance(filterType);
        var fieldInfo = filter.GetType().GetField("testNames", BindingFlags.Instance | BindingFlags.Public);
        fieldInfo = fieldInfo??filter.GetType().GetField("names", BindingFlags.Instance | BindingFlags.Public);
        if (fieldInfo == null)
        {
          ourLogger.Verbose("Could not find testNames field via reflection");
          return;
        }

        object launcher;
        if (UnityUtils.UnityVersion >= new Version(2018,1))
        {
          var enumType = testEngineAssembly.GetType("UnityEngine.TestTools.TestPlatform");
          if (enumType == null)
          {
            ourLogger.Verbose("Could not find TestPlatform field via reflection");
            return;
          }
          
          var testNameStrings = (object) myLaunch.TestNames.ToArray();
          fieldInfo.SetValue(filter, testNameStrings);
          
          var assemblyProviderType = testEditorAssembly.GetType("UnityEditor.TestTools.TestRunner.TestInEditorTestAssemblyProvider");
          var testPlatformVal = myLaunch.TestMode == TestMode.Edit ? 2 : 4; // All = 255, // 0xFF, EditMode = 2, PlayMode = 4,
          if (assemblyProviderType != null)
          {
            var assemblyProvider = Activator.CreateInstance(assemblyProviderType,
              BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null,
              new[] {Enum.ToObject(enumType, testPlatformVal)}, null); 
            ourLogger.Log(LoggingLevel.INFO, assemblyProvider.ToString());
            launcher = Activator.CreateInstance(launcherType,
              BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
              null, new[] {filter, assemblyProvider},
              null);
          }
          else
          {
            launcher = Activator.CreateInstance(launcherType,
              BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
              null, new[] {filter, Enum.ToObject(enumType, testPlatformVal)}, 
              null);
          }
        }
        else
        {
          var testNameStrings = (object) myLaunch.TestNames.ToArray();
          fieldInfo.SetValue(filter, testNameStrings);

          launcher = Activator.CreateInstance(launcherType,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            null, new[] {filter},
            null);
        }

        var runnerField = launcherType.GetField("m_EditModeRunner", BindingFlags.Instance | BindingFlags.NonPublic);
        if (runnerField == null)
        {
          ourLogger.Verbose("Could not find runnerField via reflection");
          return;
        }

        var runner = runnerField.GetValue(launcher);          
        SupportAbort(runner);

        if (!AdviseTestStarted(runner))
          return;

        if (!AdviseTestFinished(runner))
          return;

        if (!AdviseSessionFinished(runner))
          return;

        var runMethod = launcherType.GetMethod("Run", BindingFlags.Instance | BindingFlags.Public);
        if (runMethod == null)
        {
          ourLogger.Verbose("Could not find runMethod via reflection");
          return;
        }

        //run!
        runMethod.Invoke(launcher, null);
      }
      catch (Exception e)
      {
        ourLogger.Error(e, "Exception while launching Unity Editor tests.");
      }

    }

    private void SupportAbort(object runner)
    {
      var unityTestAssemblyRunnerField =
        runner.GetType().GetField("m_Runner", BindingFlags.Instance | BindingFlags.NonPublic);
      if (unityTestAssemblyRunnerField != null)
      {
        var unityTestAssemblyRunner = unityTestAssemblyRunnerField.GetValue(runner);
        var stopRunMethod = unityTestAssemblyRunner.GetType()
          .GetMethod("StopRun", BindingFlags.Instance | BindingFlags.Public);
        myLaunch.Abort.Set((lifetime, _) =>
        {
          ourLogger.Verbose("Call StopRun method via reflection.");
          var task = new RdTask<bool>();
          try
          {
            stopRunMethod.Invoke(unityTestAssemblyRunner, null);
            task.Set(true);
          }
          catch (Exception)
          {
            ourLogger.Verbose("Call StopRun method failed.");
            task.Set(false);
          }
          return task;
        });
      }
    }

    private bool AdviseSessionFinished(object runner)
    {
      var mRunFinishedEventMethodInfo= runner.GetType()
        .GetField("m_RunFinishedEvent", BindingFlags.Instance | BindingFlags.NonPublic);

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

    private bool AdviseTestStarted(object runner)
    {
      var mTestStartedEventMethodInfo = runner.GetType()
        .GetField("m_TestStartedEvent", BindingFlags.Instance | BindingFlags.NonPublic);

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

    private bool AdviseTestFinished(object runner)
    {
      var mTestFinishedEventMethodInfo = runner.GetType()
        .GetField("m_TestFinishedEvent", BindingFlags.Instance | BindingFlags.NonPublic);

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
    
    private void RunFinished(ITestResult test)
    {
      myLaunch.RunResult.Fire(new RunResult(true));
    }

    private void TestStarted(ITest test)
    {
      if (!(test is TestMethod))
        return;

      ourLogger.Verbose("TestStarted : {0}", test.FullName);
      var id = GetIdFromNUnitTest(test);

      myLaunch.TestResult.Fire(new TestResult(id, string.Empty, 0, Status.Running, GetIdFromNUnitTest(test.Parent)));
    }

    private void TestFinished(ITestResult testResult)
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
          
      myLaunch.TestResult.Fire(new TestResult(id, output,
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

    private string GetIdFromNUnitTest(ITest test)
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
