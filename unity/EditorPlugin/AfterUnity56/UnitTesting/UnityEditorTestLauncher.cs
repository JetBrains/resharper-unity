using System;
using System.Linq;
using System.Reflection;
using JetBrains.Diagnostics;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.Model;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEngine.Events;
using TestResult = JetBrains.Rider.Model.TestResult;

namespace JetBrains.Rider.Unity.Editor.AfterUnity56.UnitTesting
{
  public class UnityEditorTestLauncher
  {
    private readonly UnitTestLaunch myLaunch;

    private static readonly ILog ourLogger = Log.GetLog("RiderPlugin");
    private static string RunnerAddListener = "AddListener";

    public UnityEditorTestLauncher(UnitTestLaunch launch)
    {
      myLaunch = launch;
    }

    public void TryLaunchUnitTests()
    {
      // new way
      var success = TryLaunchUnitTestsInternal();
      if (success)
        return;

      // old way running tests works only for EditMode tests
      if (myLaunch.TestMode != TestMode.Edit)
      {
        return;
      }

      // old way
      TryLaunchUnitTestsInAssembly(myLaunch.TestFilters.SelectMany(a=>a.TestNames).ToArray());
    }

    private bool TryLaunchUnitTestsInternal()
    {
      try
      {
        var riderPackageAssembly = RiderPackageInterop.GetAssembly();
        if (riderPackageAssembly == null) return false;
        var launcherType = riderPackageAssembly.GetType("Packages.Rider.Editor.UnitTesting.RiderTestRunner");
        if (launcherType == null) return false;
        var assemblyNames = myLaunch.TestFilters.Select(a => a.AssemblyName).ToArray();
        var testNames = myLaunch.TestFilters.SelectMany(a => a.TestNames).ToArray();
        var runTestsMethod = launcherType.GetMethod("RunTests");
        if (runTestsMethod == null) return false;
        var mode = (int) myLaunch.TestMode; // 0 for Both, 1 for Edit, 2 for Play
        runTestsMethod.Invoke(null, new object[] {mode, assemblyNames, testNames, null, null, null});
        return true;
      }
      catch (Exception e)
      {
        ourLogger.Error(e, "Exception while TryLaunchUnitTestsInternal.");
      }

      return false;
    }

    private void TryLaunchUnitTestsInAssembly(string[] testNames)
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

        var launcherTypeString = "UnityEditor.TestTools.TestRunner.EditModeLauncher";
        var launcherType = testEditorAssembly.GetType(launcherTypeString);
        if (launcherType == null)
        {
          string testEditorAssemblyProperties =  testEditorAssembly.GetTypes().Select(a=>a.Name).Aggregate((a, b)=> a+ ", " + b);
          throw new NullReferenceException($"Could not find {launcherTypeString} among {testEditorAssemblyProperties}");
        }

        var filterType = testEngineAssembly.GetType("UnityEngine.TestTools.TestRunner.GUI.TestRunnerFilter");
        if (filterType == null)
        {
          string testEngineAssemblyProperties = testEngineAssembly.GetTypes().Select(a=>a.Name).Aggregate((a, b)=> a+ ", " + b);
          throw new NullReferenceException($"Could not find \"UnityEngine.TestTools.TestRunner.GUI.TestRunnerFilter\" among {testEngineAssemblyProperties}");
        }

        var filter = Activator.CreateInstance(filterType);
        var fieldInfo = filter.GetType().GetField("testNames", BindingFlags.Instance | BindingFlags.Public);
        fieldInfo = fieldInfo??filter.GetType().GetField("names", BindingFlags.Instance | BindingFlags.Public);
        if (fieldInfo == null)
        {
          ourLogger.Verbose("Could not find testNames field via reflection");
          return;
        }
        fieldInfo.SetValue(filter, testNames);

        object launcher;
        if (UnityUtils.UnityVersion >= new Version(2018, 1))
        {
          var enumType = testEngineAssembly.GetType("UnityEngine.TestTools.TestPlatform");
          if (enumType == null)
          {
            ourLogger.Verbose("Could not find TestPlatform field via reflection");
            return;
          }

          var assemblyProviderType = testEditorAssembly.GetType("UnityEditor.TestTools.TestRunner.TestInEditorTestAssemblyProvider");
          var testPlatformVal = 2; // All = 255, // 0xFF, EditMode = 2, PlayMode = 4,
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
            try
            {
              launcher = Activator.CreateInstance(launcherType,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                null, new[] {filter, Enum.ToObject(enumType, testPlatformVal)},
                null);
            }
            catch (Exception) // Unity 2019.2+ with package com.unity.test-framework v 1.0.18 and 1.1.0 ctor was changed. in v 1.1.1 it was added back for compatibility
            {
              var apiFilterType = testEditorAssembly.GetType("UnityEditor.TestTools.TestRunner.Api.Filter");
              var apiFilter = Activator.CreateInstance(apiFilterType);
              var testNamesFieldInfo = apiFilter.GetType().GetField("testNames");
              testNamesFieldInfo.SetValue(apiFilter, testNames);
              var array = Array.CreateInstance(apiFilterType, 1);
              array.SetValue(apiFilter, 0);
              launcher = Activator.CreateInstance(launcherType, array, Enum.ToObject(enumType, testPlatformVal));
            }
          }
        }
        else
        {
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

        if (!AdviseTestStarted(runner, "m_TestStartedEvent", test =>
        {
          if (!(test is TestMethod)) return;
          ourLogger.Verbose("TestStarted : {0}", test.FullName);
          var tResult = new TestResult(TestEventsSender.GetIdFromNUnitTest(test), test.Method.TypeInfo.Assembly.GetName().Name,string.Empty, 0, TestResultStatus.Running,
            TestEventsSender.GetIdFromNUnitTest(test.Parent));
          TestEventsSender.TestStarted(myLaunch, tResult);
        }))
          return;

        if (!AdviseTestFinished(runner, "m_TestFinishedEvent", result =>
        {
          if (!(result.Test is TestMethod)) return;

          TestEventsSender.TestFinished(myLaunch, TestEventsSender.GetTestResult(result));
        }))
          return;

        if (!AdviseSessionFinished(runner, "m_RunFinishedEvent", result =>
        {
          TestEventsSender.RunFinished(myLaunch);
        }))
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

    private static bool AdviseSessionFinished(object runner, string fieldName, Action<ITestResult> callback)
    {
      var mRunFinishedEventMethodInfo= runner.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

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
      addListenerMethod.Invoke(mRunFinished, new object[] {new UnityAction<ITestResult>(callback)});
      return true;
    }

    private static bool AdviseTestStarted(object runner, string fieldName, Action<ITest> callback)
    {
      var mTestStartedEventMethodInfo = runner.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

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
      addListenerMethod.Invoke(mTestStarted, new object[] {new UnityAction<ITest>(callback)});
      return true;
    }

    private static bool AdviseTestFinished(object runner, string fieldName, Action<ITestResult> callback)
    {
      var mTestFinishedEventMethodInfo = runner.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

      if (mTestFinishedEventMethodInfo == null)
      {
        ourLogger.Verbose("Could not find m_TestFinishedEvent via reflection");
        return false;
      }

      var mTestFinished = mTestFinishedEventMethodInfo.GetValue(runner);
      var addListenerMethod = mTestFinished.GetType().GetMethod(RunnerAddListener, BindingFlags.Instance | BindingFlags.Public);

      if (addListenerMethod == null)
      {
        ourLogger.Verbose("Could not find addListenerMethod via reflection");
        return false;
      }

      //subscribe for tests callbacks
      addListenerMethod.Invoke(mTestFinished, new object[] {new UnityAction<ITestResult>(callback)});
      return true;
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
  }
}
