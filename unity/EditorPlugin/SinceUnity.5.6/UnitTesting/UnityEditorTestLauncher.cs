using System;
using System.Linq;
using System.Reflection;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.Model.Unity.BackendUnity;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEngine.Events;
using TestResult = JetBrains.Rider.Model.Unity.BackendUnity.TestResult;

namespace JetBrains.Rider.Unity.Editor.UnitTesting
{
  internal class UnityEditorTestLauncher
  {
    private readonly UnitTestLaunch myLaunch;
    private readonly Lifetime myConnectionLifetime;

    private static readonly ILog ourLogger = Log.GetLog("RiderPlugin");
    private static string RunnerAddListener = "AddListener";

    public UnityEditorTestLauncher(UnitTestLaunch launch, Lifetime connectionLifetime)
    {
      myLaunch = launch;
      myConnectionLifetime = connectionLifetime;
    }

    public bool TryLaunchUnitTests()
    {
      // new way
      var success = TryLaunchUnitTestsInternal();
      if (success)
        return true;

      // old way running tests works only for EditMode tests
      if (myLaunch.TestMode != TestMode.Edit)
      {
        return false;
      }

      // old way
      return TryLaunchUnitTestsInAssembly(myLaunch.TestFilters.SelectMany(a=>a.TestNames).ToArray());
    }

    private bool TryLaunchUnitTestsInternal()
    {
      try
      {
        var riderPackageAssembly = RiderPackageInterop.GetAssembly();
        var launcherType = riderPackageAssembly?.GetType("Packages.Rider.Editor.UnitTesting.RiderTestRunner");
        if (launcherType == null) return false;
        var assemblyNames = myLaunch.TestFilters.Select(a => a.AssemblyName).ToArray();
        var testNames = myLaunch.TestFilters.SelectMany(a => a.TestNames).ToArray();
        var groupNames = myLaunch.TestFilters.SelectMany(a => a.GroupNames).ToArray();
        var categoryNames = myLaunch.TestFilters.SelectMany(a => a.TestCategories).ToArray();
        var mode = (int) myLaunch.TestMode; // 0 for Both, 1 for Edit, 2 for Play

        MethodInfo runTestsMethod;
        object[] args;

        if (myLaunch.ClientControllerInfo != null)
        {
          ourLogger.Verbose($"ClientController specified (SessionId={myLaunch.SessionId}): {myLaunch.ClientControllerInfo.TypeName}, {myLaunch.ClientControllerInfo.CodeBase}");

          runTestsMethod = launcherType.GetMethod("RunTestsWithSyncCallbacks");
          if (runTestsMethod == null)
          {
            ourLogger.Verbose($"Method 'RunTestsWithSyncCallbacks' not found in type '{launcherType.AssemblyQualifiedName}'");
            return false;
          }

          args = new object[] {myLaunch.SessionId.ToString(), mode, assemblyNames, testNames, categoryNames, groupNames, null,
            myLaunch.ClientControllerInfo.CodeBase, myLaunch.ClientControllerInfo.TypeName,
            myLaunch.ClientControllerInfo.CodeBaseDependencies?.ToArray() };
        }
        else
        {
          runTestsMethod = launcherType.GetMethod("RunTests");
          if (runTestsMethod == null)
          {
            ourLogger.Verbose($"Method 'RunTests' not found in type '{launcherType.AssemblyQualifiedName}'");
            return false;
          }

          args = new object[] {mode, assemblyNames, testNames, categoryNames, groupNames, null};
        }

        runTestsMethod.Invoke(null, args);
        return true;
      }
      catch (Exception e)
      {
        ourLogger.Error(e, "Exception while TryLaunchUnitTestsInternal.");
      }

      return false;
    }

    internal static void SupportAbortNew(UnitTestLaunch launch)
    {
      try
      {
        var riderPackageAssembly = RiderPackageInterop.GetAssembly();
        var apiType = riderPackageAssembly?.GetType("Packages.Rider.Editor.UnitTesting.RiderTestRunner");
        if (apiType == null)
        {
          ourLogger.Verbose($"Could not find RiderTestRunner in the {riderPackageAssembly}.");
          return;
        };
        
        var methodName = "CancelTestRun";
        var stopRunMethod = apiType.GetMethod(methodName); 
        if (stopRunMethod == null)
          stopRunMethod = apiType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
        if (stopRunMethod == null) 
          ourLogger.Verbose($"Could not find {methodName} in the {apiType.Name}.");

        launch.Abort.Set((lifetime, _) =>
        {
          var task = new RdTask<bool>();

          if (stopRunMethod != null)
          {
            ourLogger.Verbose($"Call {methodName} method via reflection.");
            try
            {
              stopRunMethod.Invoke(null, null);
              task.Set(true);
              if (!launch.RunStarted.HasTrueValue()) // if RunStarted never happened
                  launch.RunResult(new RunResult(false));
            }
            catch (Exception)
            {
              ourLogger.Verbose($"Call {methodName} method failed.");
              task.Set(false);
            }
          }
          else
            task.Set(false);

          return task;
        });
      }
      catch (Exception e)
      {
        ourLogger.Error(e, "Unexpected exception in SupportAbortNew");
      }
    }

    private bool TryLaunchUnitTestsInAssembly(string[] testNames)
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
          return false;
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
          return false;
        }
        fieldInfo.SetValue(filter, testNames);

        var clientController = ClientControllerWrapper.TryCreate(myLaunch.SessionId.ToString(), myLaunch.ClientControllerInfo);
        clientController?.OnSessionStarted();

        object launcher;
        if (UnityUtils.UnityVersion >= new Version(2018, 1))
        {
          var enumType = testEngineAssembly.GetType("UnityEngine.TestTools.TestPlatform");
          if (enumType == null)
          {
            ourLogger.Verbose("Could not find TestPlatform field via reflection");
            return false;
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
          return false;
        }

        var runner = runnerField.GetValue(launcher);
        SupportAbort(runner);

        var runLifetimeDef = Lifetime.Define(myConnectionLifetime);
        runLifetimeDef.Lifetime.OnTermination(() =>
        {
          if (myConnectionLifetime.IsNotAlive)
            TestEventsSender.RunFinished(myLaunch, new RunResult(false));
        });

        var runStarted = false;
        try
        {
          if (!AdviseTestStarted(runner, "m_TestStartedEvent", test =>
          {
            if (!(test is TestMethod)) return;
            ourLogger.Verbose("TestStarted : {0}", test.FullName);

            var testId = TestEventsSender.GetIdFromNUnitTest(test);
            var parentId = TestEventsSender.GetIdFromNUnitTest(test.Parent);
            var tResult = new TestResult(testId, test.Method.TypeInfo.Assembly.GetName().Name,string.Empty, 0, Status.Running, parentId);

            clientController?.OnTestStarted(testId);
            TestEventsSender.TestStarted(myLaunch, tResult);
          }))
            return false;

          if (!AdviseTestFinished(runner, "m_TestFinishedEvent", result =>
          {
            if (!(result.Test is TestMethod)) return;

            clientController?.OnTestFinished();
            TestEventsSender.TestFinished(myLaunch, TestEventsSender.GetTestResult(result));
          }))
            return false;

          if (!AdviseSessionFinished(runner, "m_RunFinishedEvent", result =>
          {
            clientController?.OnSessionFinished();
            runLifetimeDef.Terminate();
            var runResult = new RunResult(Equals(result.ResultState, ResultState.Success));
            TestEventsSender.RunFinished(myLaunch, runResult);
          }))
            return false;

          var runMethod = launcherType.GetMethod("Run", BindingFlags.Instance | BindingFlags.Public);
          if (runMethod == null)
          {
            ourLogger.Verbose("Could not find runMethod via reflection");
            return false;
          }

          //run!
          runMethod.Invoke(launcher, null);
          runStarted = true;
          return true;
        }
        finally
        {
          if (!runStarted)
            runLifetimeDef.Terminate();
        }
      }
      catch (Exception e)
      {
        ourLogger.Error(e, "Exception while launching Unity Editor tests.");
        return false;
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
      try
      {
        var unityTestAssemblyRunnerField =
          runner.GetType().GetField("m_Runner", BindingFlags.Instance | BindingFlags.NonPublic);
        if (unityTestAssemblyRunnerField == null)
        {
          ourLogger.Verbose("Could not find m_Runner field.");
          return;
        }
        var unityTestAssemblyRunner = unityTestAssemblyRunnerField.GetValue(runner);
        var stopRunMethod = unityTestAssemblyRunner.GetType()
          .GetMethod("StopRun", BindingFlags.Instance | BindingFlags.Public);
        if (stopRunMethod == null)
        {
          ourLogger.Verbose("Could not find StopRun method.");
          return;
        }
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
      catch (Exception e)
      {
        ourLogger.Error(e, "Unexpected exception in SupportAbort.");
      }
    }
  }
}