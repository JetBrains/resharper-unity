using System;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Rd.Tasks;
using UnityEditor;
using UnityEngine.SceneManagement;
#if UNITY_2019_2_OR_NEWER
using System.IO;
using System.Linq;
using UnityEngine;
#endif

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
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Running tests during the Play mode is not possible.");
        var testLauncher = new UnityEditorTestLauncher(model.UnitTestLaunch.Value, connectionLifetime);
        return testLauncher.TryLaunchUnitTests();
      });
      
      GetUnsavedChangesInScenes(modelAndLifetime);
    }

    private static void GetUnsavedChangesInScenes(UnityModelAndLifetime modelAndLifetime)
    {
        modelAndLifetime.Model.HasUnsavedState.Set(rdVoid => 
        {
            var count = SceneManager.sceneCount;
            for (var i = 0; i < count; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                    return true;
            }
#if UNITY_2019_2_OR_NEWER
            //Example of ScriptableObject which has its state, independent from the scenes
            // Add this script to Assets
            // Create an instance by `Assets > Create > ScriptableObjects > SpawnManagerScriptableObject`
            // Change SerializableFields in the UnityEditor
            /* 
             [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/SpawnManagerScriptableObject", order = 1)]
public class SpawnManagerScriptableObject : ScriptableObject
{
    public string prefabName;

    public int numberOfPrefabsToCreate;
    public Vector3[] spawnPoints;
}
             */
            var hasDirtyUserAssets = Resources.FindObjectsOfTypeAll<UnityEngine.Object>()
                .Any(a =>
                {
                    if (!EditorUtility.IsDirty(a))
                        return false;
                    var assetPath = AssetDatabase.GetAssetPath(a);

                    if (string.IsNullOrEmpty(assetPath))
                        return false;
                    
                    return File.Exists(Path.GetFullPath(assetPath));
                });

            return hasDirtyUserAssets;
#else
            return false;
#endif
        } );
    }
  }
}