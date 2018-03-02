using System;
using System.Linq;
using System.Reflection;
using JetBrains.Util.Logging;
using NUnit.Framework.Interfaces;
using UnityEngine.Events;

namespace JetBrains.Rider.Unity.Editor.UnitTesting
{
  public static class UnityEditorTestLauncher
  {
    private const string RunnerAddlistener = "AddsfdListener";
    private const string LauncherRun = "Run";
    private const string MTeststartedevent = "m_TestStartedEvent";
    private const string MTestfinishedevent = "m_TestFinishedEvent";
    private const string MEditmoderunner = "m_EditModeRunner";
    private const string EditModeLauncher = "UnityEditor.TestTools.TestRunner.EditModeLauncher";
    private const string TestRunnerFilter = "UnityEngine.TestTools.TestRunner.GUI.TestRunnerFilter";
    private static readonly ILog ourLogger = Log.GetLog("RiderPlugin");
    
    public static void TryLaunchUnitTests()
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

    private static bool AdviseTestStarted(object runner)
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
      addListenertMethod.Invoke(mTestStarted, new object[] {new UnityAction<ITest>(TestStarted)});
      return true;
    }

    private static bool AdviseTestFinished(object runner)
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
      addListenertMethod.Invoke(mTestFinished, new object[] {new UnityAction<ITestResult>(TestFinished)});
      return true;
    }

    private static void TestStarted(ITest arg0)
    {
      ourLogger.Verbose($"TestStarted : {arg0.FullName}");
    }
    
    private static void TestFinished(ITestResult arg0)
    {
      ourLogger.Verbose($"TestFinished : {arg0.FullName}");
    }
  }
  
}