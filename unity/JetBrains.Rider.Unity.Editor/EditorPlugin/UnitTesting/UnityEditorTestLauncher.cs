using System;
using System.Linq;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.Util.Logging;
#if !(UNITY_5_5 || UNITY_4_7)
using NUnit.Framework.Internal;
using NUnit.Framework.Interfaces;
#endif
using UnityEngine.Events;
using TestResult = JetBrains.Platform.Unity.EditorPluginModel.TestResult;

namespace JetBrains.Rider.Unity.Editor.UnitTesting
{
  public class UnityEditorTestLauncher
  {
    private readonly UnitTestLaunch myLaunch;
    private const string RunnerAddlistener = "AddListener";
    private const string LauncherRun = "Run";
    private const string MTeststartedevent = "m_TestStartedEvent";
    private const string MRunfinishedevent = "m_RunFinishedEvent";
    private const string MTestfinishedevent = "m_TestFinishedEvent";
    private const string MEditmoderunner = "m_EditModeRunner";
    private const string TestNames = "testNames";
    private const string EditModeLauncher = "UnityEditor.TestTools.TestRunner.EditModeLauncher";
    private const string TestRunnerFilter = "UnityEngine.TestTools.TestRunner.GUI.TestRunnerFilter";
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
          ourLogger.Verbose("Could not find UnityEditor.TestRunner or UnityEngine.TestRunner assemblies in current AppDomain");
          return;
        }
        
        var launcherType = testEditorAssembly.GetType(EditModeLauncher);
        var filterType = testEngineAssembly.GetType(TestRunnerFilter);
        if (launcherType == null || filterType == null)
        {
          ourLogger.Verbose("Could not find launcherType or filterType via reflection");
          return;
        }
        
        var filter = Activator.CreateInstance(filterType);
        var fieldInfo = filter.GetType().GetField(TestNames, BindingFlags.Instance | BindingFlags.Public);
        if(fieldInfo == null)
        {
          ourLogger.Verbose("Could not find testNames field via reflection");
          return;
        }
        
        var testNameStrings = (object)myLaunch.TestNames.ToArray();
        fieldInfo.SetValue(filter, testNameStrings);

        var launcher = Activator.CreateInstance(launcherType,
          BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
          null, new[] {filter},
          null);

        var runnerField = launcherType.GetField(MEditmoderunner, BindingFlags.Instance | BindingFlags.NonPublic);
        if (runnerField == null)
        {
          ourLogger.Verbose("Could not find runnerField via reflection");
          return;
        }
        
        var runner = runnerField.GetValue(launcher);
        
        if (!AdviseTestStarted(runner)) 
          return;
        
        if (!AdviseTestFinished(runner)) 
          return;

        if(!AdviseSessionFinished(runner))
          return;
        
        var runMethod = launcherType.GetMethod(LauncherRun, BindingFlags.Instance | BindingFlags.Public);
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

    private bool AdviseSessionFinished(object runner)
    {
      var mTestStartedEventMethodInfo = runner.GetType()
        .GetField(MRunfinishedevent, BindingFlags.Instance | BindingFlags.NonPublic);

      if (mTestStartedEventMethodInfo == null)
      {
        ourLogger.Verbose("Could not find mRunFinishedEventMethodInfo via reflection");
        return false;
      }

      var mTestStarted = mTestStartedEventMethodInfo.GetValue(runner);
      var addListenertMethod =
        mTestStarted.GetType().GetMethod(RunnerAddlistener, BindingFlags.Instance | BindingFlags.Public);

      if (addListenertMethod == null)
      {
        ourLogger.Verbose("Could not find addListenertMethod via reflection");
        return false;
      }

      //subscribe for tests callbacks
#if !(UNITY_5_5 || UNITY_4_7)
      addListenertMethod.Invoke(mTestStarted, new object[] {new UnityAction<ITestResult>(RunFinished)});
#endif
      return true;
    }
    
    private bool AdviseTestStarted(object runner)
    {
      var mTestStartedEventMethodInfo = runner.GetType()
        .GetField(MTeststartedevent, BindingFlags.Instance | BindingFlags.NonPublic);

      if (mTestStartedEventMethodInfo == null)
      {
        ourLogger.Verbose("Could not find mTestStartedEventMethodInfo via reflection");
        return false;
      }

      var mTestStarted = mTestStartedEventMethodInfo.GetValue(runner);
      var addListenertMethod =
        mTestStarted.GetType().GetMethod(RunnerAddlistener, BindingFlags.Instance | BindingFlags.Public);

      if (addListenertMethod == null)
      {
        ourLogger.Verbose("Could not find addListenertMethod via reflection");
        return false;
      }

      //subscribe for tests callbacks
#if !(UNITY_5_5 || UNITY_4_7)
      addListenertMethod.Invoke(mTestStarted, new object[] {new UnityAction<ITest>(TestStarted)});
#endif
      return true;
    }

    private bool AdviseTestFinished(object runner)
    {
      var mTestFinishedEventMethodInfo = runner.GetType()
        .GetField(MTestfinishedevent, BindingFlags.Instance | BindingFlags.NonPublic);

      if (mTestFinishedEventMethodInfo == null)
      {
        ourLogger.Verbose("Could not find MTestfinishedevent via reflection");
        return false;
      }

      var mTestFinished = mTestFinishedEventMethodInfo.GetValue(runner);
      var addListenertMethod =
        mTestFinished.GetType().GetMethod(RunnerAddlistener, BindingFlags.Instance | BindingFlags.Public);

      if (addListenertMethod == null)
      {
        ourLogger.Verbose("Could not find addListenertMethod via reflection");
        return false;
      }

      //subscribe for tests callbacks
#if !(UNITY_5_5 || UNITY_4_7)
      addListenertMethod.Invoke(mTestFinished, new object[] {new UnityAction<ITestResult>(TestFinished)});
#endif
      return true;
    }
#if !(UNITY_5_5 || UNITY_4_7)
   
    private void RunFinished(ITestResult test)
    {
      myLaunch.RunResult.Fire(new RunResult(true));
    }
    
    private void TestStarted(ITest test)
    {
      if (!(test is TestMethod))
        return;

      ourLogger.Verbose($"TestStarted : {test.FullName}");
      var id = GetIdFromNUnitTest(test);
      
      myLaunch.TestResult.Fire(new TestResult(id, string.Empty, 0, Status.Running));
    }
    
    private void TestFinished(ITestResult testResult)
    {
      var test = testResult.Test;
      if (!(test is TestMethod))
        return;

      ourLogger.Verbose($"TestFinished : {test.FullName}");
      var id = GetIdFromNUnitTest(test);

      var output = ExtractOutput(testResult);
      myLaunch.TestResult.Fire(new TestResult(id, output, (int)TimeSpan.FromMilliseconds(testResult.Duration).TotalMilliseconds, Equals(testResult.ResultState, ResultState.Success) ? Status.Passed : Status.Failed));
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

    [NotNull]
    private string GetIdFromNUnitTest(ITest test)
    {
      var testMethod = test as TestMethod;
      if(testMethod == null)
        throw new ArgumentException("Could not get id from NUnit test {0}", test.FullName);

      var methodName = testMethod.Name;
      var className = testMethod.ClassName;

      return string.Format("{0}.{1}", className, methodName);
    }
#endif
  }
}