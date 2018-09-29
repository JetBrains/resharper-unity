using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Platform.RdFramework.Tasks;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.Util;
using JetBrains.Util.Logging;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JetBrains.Rider.Unity.Editor.UnitTesting
{
  public class UnityEditorTestLauncher
  {
    private readonly UnitTestLaunch myLaunch;
    
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
          if (PluginEntryPoint.PlayModeSavedState == PluginEntryPoint.PlayModeState.Playing)
          {
            var bootstrapSceneInfo = runnerSettings.GetType().GetField("bootstrapScene", BindingFlags.Instance | BindingFlags.Public);
            bootstrapSceneInfo.SetValue(runnerSettings, activeScene.path);
            var originalSceneInfo = runnerSettings.GetType().GetField("originalScene", BindingFlags.Instance | BindingFlags.Public);
            originalSceneInfo.SetValue(runnerSettings, activeScene.path);
          }
          
          var playModeLauncher = Activator.CreateInstance(launcherType,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            null, new[] {runnerSettings},
            null);

          //var interfaceType = testEngineAssembly.GetType("UnityEngine.TestTools.TestRunner.ITestRunnerListener");
          //playModeLauncher.GetType().GetMethod("AddEventHandler").AddEventHandler<ResultUpdater>();

          PlayModeLauncherRun(playModeLauncher, runnerSettings, testEditorAssembly, testEngineAssembly);
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

          if (!TestListenersStarter.AdviseTestStarted(runner, "m_TestStartedEvent"))
            return;

          if (!TestListenersStarter.AdviseTestFinished(runner, "m_TestFinishedEvent"))
            return;

          if (!TestListenersStarter.AdviseSessionFinished(runner, "m_RunFinishedEvent"))
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

    private void PlayModeLauncherRun(object playModeLauncher, object runnerSettings,
      Assembly testEditorAssembly, Assembly testEngineAssembly)
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
      
      var runnerSetupAction = PlayModeRunnerSetupAction(runnerSettings, testEditorAssembly, testEngineAssembly);
      playModeLauncher.GetType().GetField("IsRunning").SetValue(null, true);
      //ConsoleWindow.SetConsoleErrorPause(false);
      Application.runInBackground = true;
      var sceneName = (string) playModeLauncher.GetType().GetMethod("CreateSceneName").Invoke(playModeLauncher, new object[]{});
      var CreateBootstrapSceneMethodResult = playModeLauncher.GetType()
        .GetMethod("CreateBootstrapScene", BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        .Invoke(playModeLauncher, new[] {sceneName, runnerSetupAction });
      playModeLauncher.GetType().GetField("m_Scene", BindingFlags.NonPublic| BindingFlags.Instance)
        .SetValue(playModeLauncher, CreateBootstrapSceneMethodResult);
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

    private object PlayModeRunnerSetupAction(object runnerSettings, Assembly testEditorAssembly, Assembly editorAssembly)
    {
          var action1 = new Action<object>(runner =>
          {
//            runner.AddEventHandlerMonoBehaviour<PlayModeRunnerCallback>();
//            runner.AddEventHandlerScriptableObject<TestRunnerCallback>();
//            runner.AddEventHandlerScriptableObject<CallbacksDelegatorListener>();
            var playmodeTestsControllerExtensions = testEditorAssembly.GetType("UnityEditor.TestTools.TestRunner.PlaymodeTestsControllerExtensions");
            var playModeRunnerCallbackType = editorAssembly.GetType("UnityEngine.TestTools.TestRunner.Callbacks.PlayModeRunnerCallback");
            var callbacksDelegatorListenerType = testEditorAssembly.GetType("UnityEditor.TestTools.TestRunner.Api.CallbacksDelegatorListener");
            // stops Play after tests
            var testRunnerCallbackType = testEditorAssembly.GetType("UnityEditor.TestTools.TestRunner.TestRunnerCallback");
            
            playmodeTestsControllerExtensions.GetMethod("AddEventHandlerMonoBehaviour",  
              BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
              .MakeGenericMethod(playModeRunnerCallbackType).Invoke(null, new object[] { runner });
            var method = playmodeTestsControllerExtensions
                .GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single(a => a.Name=="AddEventHandlerScriptableObject" && a.IsGenericMethod);
            method.MakeGenericMethod(testRunnerCallbackType).Invoke(null, new object[] { runner });;
            method.MakeGenericMethod(callbacksDelegatorListenerType).Invoke(null, new object[] { runner });
//            UnityEventTools.AddPersistentListener((UnityEvent<ITest>) runner.GetType().GetField("testStartedEvent", 
//              BindingFlags.NonPublic| BindingFlags.Instance).GetValue(runner), TestStarted);
//            UnityEventTools.AddPersistentListener((UnityEvent<ITestResult>) runner.GetType().GetField("testFinishedEvent",
//              BindingFlags.NonPublic| BindingFlags.Instance).GetValue(runner), TestFinished);
//            UnityEventTools.AddPersistentListener((UnityEvent<ITestResult>) runner.GetType().GetField("runFinishedEvent",
//              BindingFlags.NonPublic| BindingFlags.Instance).GetValue(runner), RunFinished);
            
            //if (SubscribePlayModeListeners(runner)) return; // maybe no need to subscribe when creating

//          runner.settings = runnerSettings;
            runner.GetType().GetField("settings").SetValue(runner, runnerSettings);
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
  }
}
