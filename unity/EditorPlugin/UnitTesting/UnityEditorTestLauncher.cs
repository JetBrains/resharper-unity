using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using JetBrains.Platform.RdFramework.Tasks;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.Util;
using JetBrains.Util.Logging;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
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

        var launcherTypeString = myLaunch.TestMode == TestMode.Edit ? 
          "UnityEditor.TestTools.TestRunner.EditModeLauncher" : 
          "UnityEditor.TestTools.TestRunner.PlaymodeLauncher";
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
        
        var testNameStrings = (object) myLaunch.TestNames.ToArray();
        fieldInfo.SetValue(filter, testNameStrings);

        if (myLaunch.TestMode == TestMode.Play)
        {
          var playmodeTestsControllerSettingsTypeString = "UnityEngine.TestTools.TestRunner.PlaymodeTestsControllerSettings";
          var playmodeTestsControllerSettingsType = testEngineAssembly.GetType(playmodeTestsControllerSettingsTypeString);

          var runnerSettings = playmodeTestsControllerSettingsType.GetMethod("CreateRunnerSettings")
            .Invoke(null, new[] {filter});
          var activeScene = SceneManager.GetActiveScene();
          var bootstrapSceneInfo = runnerSettings.GetType().GetField("bootstrapScene", BindingFlags.Instance | BindingFlags.Public);
          bootstrapSceneInfo.SetValue(runnerSettings, activeScene.path);
          var originalSceneInfo = runnerSettings.GetType().GetField("originalScene", BindingFlags.Instance | BindingFlags.Public);
          originalSceneInfo.SetValue(runnerSettings, activeScene.path);
          
          var playModeLauncher = Activator.CreateInstance(launcherType,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            null, new[] {runnerSettings},
            null);

          //var interfaceType = testEngineAssembly.GetType("UnityEngine.TestTools.TestRunner.ITestRunnerListener");
          //playModeLauncher.GetType().GetMethod("AddEventHandler").AddEventHandler<ResultUpdater>();
          
          var playModeTestsControllerTypeString = "UnityEngine.TestTools.TestRunner.PlaymodeTestsController";
          var playModeTestsControllerType = testEngineAssembly.GetType(playModeTestsControllerTypeString);
          
          PlayModeLauncherRun(playModeLauncher, playModeTestsControllerType, runnerSettings, testEditorAssembly);
        }
        else
        {
          object launcher;
          if (UnityUtils.UnityVersion >= new Version(2018, 1))
          {
            var enumType = testEngineAssembly.GetType("UnityEngine.TestTools.TestPlatform");
            if (enumType == null)
            {
              ourLogger.Verbose("Could not find TestPlatform field via reflection");
              return;
            }

            var assemblyProviderType =
              testEditorAssembly.GetType("UnityEditor.TestTools.TestRunner.TestInEditorTestAssemblyProvider");
            var testPlatformVal =
              myLaunch.TestMode == TestMode.Edit ? 2 : 4; // All = 255, // 0xFF, EditMode = 2, PlayMode = 4,
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

          if (!AdviseTestStarted(runner, "m_TestStartedEvent"))
            return;

          if (!AdviseTestFinished(runner, "m_TestFinishedEvent"))
            return;

          if (!AdviseSessionFinished(runner, "m_RunFinishedEvent"))
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
      }
      catch (Exception e)
      {
        ourLogger.Error(e, "Exception while launching Unity Editor tests.");
      }
    }

    private void PlayModeLauncherRun(object playModeLauncher, Type playModeTestsControllerType, object runnerSettings,
      Assembly testEditorAssembly)
    {
//      Unity 2018.3.0b1
//      PlaymodeLauncher.IsRunning = true;
//      ConsoleWindow.SetConsoleErrorPause(false);
//      Application.runInBackground = true;
//      string sceneName = this.CreateSceneName();
//      this.m_Scene = this.CreateBootstrapScene(sceneName, runnerSetupAction));
//      if (this.m_Settings.sceneBased)
//      {
//        var buildSettingsSceneList = new List<EditorBuildSettingsScene>()
//        {
//          new EditorBuildSettingsScene(sceneName, true)
//        };
//        buildSettingsSceneList.AddRange(EditorBuildSettings.scenes);
//        EditorBuildSettings.scenes = buildSettingsSceneList.ToArray();
//      }
//      EditorApplication.update += this.UpdateCallback;
      
      var runnerSetupAction = RunnerSetupAction(playModeTestsControllerType, runnerSettings, testEditorAssembly);
      playModeLauncher.GetType().GetField("IsRunning").SetValue(null, true);
      //ConsoleWindow.SetConsoleErrorPause(false);
      Application.runInBackground = true;
      var sceneName = (string) playModeLauncher.GetType().GetMethod("CreateSceneName").Invoke(playModeLauncher, new object[]{});
      var CreateBootstrapSceneMethodResult = playModeLauncher.GetType().GetMethod("CreateBootstrapScene")
        .Invoke(playModeLauncher, new[] {sceneName, runnerSetupAction });
      playModeLauncher.GetType().GetField("m_Scene").SetValue(playModeLauncher, CreateBootstrapSceneMethodResult);
      var sceneBased = (bool) runnerSettings.GetType().GetField("sceneBased").GetValue(runnerSettings);
      if (sceneBased)
      {
        var buildSettingsSceneList = new List<EditorBuildSettingsScene>()
        {
          new EditorBuildSettingsScene(sceneName, true)
        };
        buildSettingsSceneList.AddRange(EditorBuildSettings.scenes);
        EditorBuildSettings.scenes = buildSettingsSceneList.ToArray();        
      }

      var updateCallBack = playModeLauncher.GetType().GetMethod("UpdateCallback");
      
      EditorApplication.update += ()=> { updateCallBack.Invoke(playModeLauncher, new object[]{}); };
    }

    private object RunnerSetupAction(Type playModeTestsControllerType, object runnerSettings,
      Assembly testEditorAssembly)
    {
          var action1 = new Action<object>(runner =>
          {
            var playmodeTestsControllerExtensions = testEditorAssembly.GetType("UnityEditor.TestTools.TestRunner.PlaymodeTestsControllerExtensions");
            var PlayModeRunnerCallback = testEditorAssembly.GetType("UnityEngine.TestTools.TestRunner.Callbacks.PlayModeRunnerCallback");
            var methods = playmodeTestsControllerExtensions.GetMethods();
            methods.Single(a => a.Name == "AddEventHandlerMonoBehaviour").Invoke(null, new object[] { });
            methods.Single(a => a.Name == "CallbacksDelegatorListener").Invoke(null, new object[] { });

            UnityEventTools.AddPersistentListener((UnityEvent<ITest>) runner.GetType().GetField("testStartedEvent").GetValue(runner), TestStarted);
            UnityEventTools.AddPersistentListener((UnityEvent<ITestResult>) runner.GetType().GetField("testFinishedEvent").GetValue(runner), TestFinished);
            //UnityEventTools.AddPersistentListener((UnityEvent<ITest>) runner.runStartedEvent, RunStarted);
            UnityEventTools.AddPersistentListener((UnityEvent<ITestResult>) runner.GetType().GetField("runFinishedEvent").GetValue(runner), RunFinished);

            runner.GetType().GetField("settings").SetValue(runner, runnerSettings);
//            runner.AddEventHandlerMonoBehaviour<PlayModeRunnerCallback>();
//            runner.AddEventHandlerScriptableObject<TestRunnerCallback>();
//            runner.AddEventHandlerScriptableObject<CallbacksDelegatorListener>();
//            
//            UnityEventTools.AddPersistentListener((UnityEvent<ITest>) runner.testStartedEvent, TestStarted);
//            UnityEventTools.AddPersistentListener((UnityEvent<ITestResult>) runner.testFinishedEvent, TestFinished);
//            //UnityEventTools.AddPersistentListener((UnityEvent<ITest>) runner.runStartedEvent, RunStarted);
//            UnityEventTools.AddPersistentListener((UnityEvent<ITestResult>) runner.runFinishedEvent, RunFinished);
//
//            runner.settings = runnerSettings;
          });

      return action1;
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

    private bool AdviseSessionFinished(object runner, string fieldName)
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

    private bool AdviseTestStarted(object runner, string fieldName)
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

    private bool AdviseTestFinished(object runner, string fieldName)
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
