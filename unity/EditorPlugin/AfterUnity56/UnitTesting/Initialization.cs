using System;
using System.Collections.Generic;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Rd.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace JetBrains.Rider.Unity.Editor.AfterUnity56.UnitTesting
{
  public static class Initialization
  {
    private static readonly ILog ourLogger = Log.GetLog("UnitTesting.Initialization");
    
    public static void OnModelInitializationHandler(UnityModelAndLifetime modelAndLifetime)
    {
      ourLogger.Verbose("AdviseUnitTestLaunch");
      var model = modelAndLifetime.Model;
      var connectionLifetime = modelAndLifetime.Lifetime;
      
      model.GetCompilationResult.Set(_ => !EditorUtility.scriptCompilationFailed);

#if !UNITY_5_6 // before 5.6 this file is not included at all
      CompiledAssembliesTracker.Init(modelAndLifetime);
#endif

      model.UnitTestLaunch.Advise(connectionLifetime, launch =>
      {
        new TestEventsSender(launch);
        UnityEditorTestLauncher.SupportAbortNew(launch); // TestFramework 1.2.x
      });
      
      model.RunUnitTestLaunch.Set(rdVoid =>
      {
        if (!model.UnitTestLaunch.HasValue()) return false;
        var testLauncher = new UnityEditorTestLauncher(model.UnitTestLaunch.Value, connectionLifetime);
        return testLauncher.TryLaunchUnitTests();
      });
      
      GetUnsavedChangesInScenes(modelAndLifetime);
      SaveScenes(modelAndLifetime);
    }

    private static void SaveScenes(UnityModelAndLifetime modelAndLifetime)
    {
        modelAndLifetime.Model.SaveScenes.Set(rdVoid =>
        {
            try
            {
                var count = SceneManager.sceneCount;
                var scenes = new List<Scene>();
                for (var i = 0; i < count; i++)
                {
                    var sceneAtIndex = SceneManager.GetSceneAt(i);
                    if (sceneAtIndex.isDirty)
                    {
                        scenes.Add(sceneAtIndex);
                    }
                }

                EditorSceneManager.SaveScenes(scenes.ToArray());
                return true;
            }
            catch (Exception e)
            {
                ourLogger.Error("Exception during SaveScenes request", e);
                return false;
            }
        });
    }

    private static void GetUnsavedChangesInScenes(UnityModelAndLifetime modelAndLifetime)
    {
        modelAndLifetime.Model.HasUnsavedScenes.Set(rdVoid => 
        {
            var count = SceneManager.sceneCount;
            for (var i = 0; i < count; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                    return true;
            }
            return false;
        } );
    }
  }
}