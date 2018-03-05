using System;
using System.Linq;
using System.Reflection;
using JetBrains.Platform.Unity.Model;
using JetBrains.Util.Logging;
#if !(UNITY_5_5 || UNITY_4_7)
using NUnit.Framework.Interfaces;
#endif
using UnityEngine.Events;

namespace JetBrains.Rider.Unity.Editor.UnitTesting
{
  public class UnityEditorTestLauncher
  {
    private readonly UnitTestLaunch myLaunch;
    private const string RunnerAddlistener = "AddsfdListener";
    private const string LauncherRun = "Run";
    private const string MTeststartedevent = "m_TestStartedEvent";
    private const string MTestfinishedevent = "m_TestFinishedEvent";
    private const string MEditmoderunner = "m_EditModeRunner";
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
    private void TestStarted(ITest test)
    {
      ourLogger.Verbose($"TestStarted : {test.FullName}");
      myLaunch.TestResult.Fire(new TestResult(test.FullName, Status.Running));
    }
    
    private void TestFinished(ITestResult test)
    {
      ourLogger.Verbose($"TestFinished : {test.FullName}");
      myLaunch.TestResult.Fire(new TestResult(test.FullName, Equals(test.ResultState, ResultState.Success) ? Status.Running : Status.Failed));
    }
#endif
  }
}