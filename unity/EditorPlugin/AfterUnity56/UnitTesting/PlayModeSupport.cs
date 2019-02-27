using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JetBrains.Rider.Unity.Editor.AfterUnity56.UnitTesting
{
  public static class PlayModeSupport
  {
    private static readonly ILog ourLogger = Log.GetLog("RiderPlugin");
    
    public static void PlayModeLauncherRun(object filter, Type launcherType, Assembly testEditorAssembly, Assembly testEngineAssembly)
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
      
      var playModeTestsControllerTypeString = "UnityEngine.TestTools.TestRunner.PlaymodeTestsController";
      var playModeTestsControllerType = testEngineAssembly.GetType(playModeTestsControllerTypeString);
      
      var runnerSetupActionObject = PlayModeRunnerSetupAction(runnerSettings, testEditorAssembly, testEngineAssembly, playModeTestsControllerType);
      var runnerSetupActionType = typeof(Action<>).MakeGenericType(playModeTestsControllerType);
      ourLogger.Verbose("ConvertType");
      var runnerSetupAction = ConvertDelegateType(runnerSetupActionObject, runnerSetupActionType);
      ourLogger.Verbose("AfterConvertType");

      try
      {
        // doesn't exist in Unity 5.6
        playModeLauncher.GetType().GetField("IsRunning").SetValue(null, true);
      }
      catch (Exception e)
      {
        ourLogger.Warn(e);
      }
      
      //ConsoleWindow.SetConsoleErrorPause(false);
      Application.runInBackground = true;
      var sceneName = (string) playModeLauncher.GetType().GetMethod("CreateSceneName").Invoke(playModeLauncher, new object[]{});
      var createBootstrapSceneMethod = playModeLauncher.GetType()
        .GetMethod("CreateBootstrapScene", BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      var createBootstrapSceneMethodResult = createBootstrapSceneMethod.Invoke(playModeLauncher, new[] {sceneName, (object)runnerSetupAction });
      playModeLauncher.GetType().GetField("m_Scene", BindingFlags.NonPublic| BindingFlags.Instance)
        .SetValue(playModeLauncher, createBootstrapSceneMethodResult);
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
      playModeLauncher.GetType().GetField("m_InitPlaying", BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(playModeLauncher, 3); // Unity 2018.2
    }

    private static object ConvertDelegateType(Action<object> action, Type targetType)
    {
      var ctor = targetType.GetConstructors().Single();
      var invoke = action.GetType().GetMethod("Invoke");
      return ctor.Invoke(new object[] { action, invoke.MethodHandle.GetFunctionPointer() });
    }

    
    private static Action<object> PlayModeRunnerSetupAction(object runnerSettings, Assembly testEditorAssembly,
      Assembly editorAssembly, Type playModeTestsControllerType)
    {
      var action = new Action<object>(r =>
      {
        var runner = Convert.ChangeType(r, playModeTestsControllerType);

//            runner.AddEventHandlerMonoBehaviour<PlayModeRunnerCallback>();
//            runner.AddEventHandlerScriptableObject<TestRunnerCallback>();
//            runner.AddEventHandlerScriptableObject<CallbacksDelegatorListener>();
        var playmodeTestsControllerExtensions =
          testEditorAssembly.GetType("UnityEditor.TestTools.TestRunner.PlaymodeTestsControllerExtensions");
        var playModeRunnerCallbackType =
          editorAssembly.GetType("UnityEngine.TestTools.TestRunner.Callbacks.PlayModeRunnerCallback");
        var callbacksDelegatorListenerType =
          testEditorAssembly.GetType("UnityEditor.TestTools.TestRunner.Api.CallbacksDelegatorListener");
        // stops Play after tests
        var testRunnerCallbackType = testEditorAssembly.GetType("UnityEditor.TestTools.TestRunner.TestRunnerCallback");

        playmodeTestsControllerExtensions.GetMethod("AddEventHandlerMonoBehaviour",
            BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
          .MakeGenericMethod(playModeRunnerCallbackType).Invoke(null, new[] {runner});
        var method = playmodeTestsControllerExtensions
          .GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
          .Single(a => a.Name == "AddEventHandlerScriptableObject" && a.IsGenericMethod);
        method.MakeGenericMethod(testRunnerCallbackType).Invoke(null, new[] {runner});

        try
        {
          // exists in Unity 2018.3.x
          method.MakeGenericMethod(callbacksDelegatorListenerType).Invoke(null, new[] {runner});
        }
        catch (Exception e)
        {
          ourLogger.Warn(e);
        }

        var collector = TestEventsCollector.Instance;
        collector.SetupPersistentListeners(runner);

//            runner.settings = runnerSettings;
        runner.GetType().GetField("settings").SetValue(runner, runnerSettings);
      });

      return action;
    }
  }
}