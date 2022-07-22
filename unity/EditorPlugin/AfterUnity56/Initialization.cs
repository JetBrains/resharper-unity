using System;
using System.IO;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Rd.Tasks;
using UnityEditor;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using System.Linq;
using System.Reflection;
using JetBrains.Rider.Unity.Editor.AfterUnity56.UnitTesting;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.AfterUnity56
{
    public static class Initialization
    {
        private static readonly ILog ourLogger = Log.GetLog("Initialization");
#if !UNITY_2019_2_OR_NEWER
        private static MethodInfo ourIsDirtyMethodInfo;
#endif
        public static void OnModelInitializationHandler(UnityModelAndLifetime modelAndLifetime)
        {
            ourLogger.Verbose("AdviseUnitTestLaunch");
            var model = modelAndLifetime.Model;
            var connectionLifetime = modelAndLifetime.Lifetime;

            model.GetCompilationResult.Set(_ => !EditorUtility.scriptCompilationFailed);

#if !UNITY_5_6 // only need to ignore 5.6, because before 5.6 this whole file is not included
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

            HasUnsavedChanges(modelAndLifetime);
        }

        private static void HasUnsavedChanges(UnityModelAndLifetime modelAndLifetime)
        {
            modelAndLifetime.Model.HasUnsavedState.Set(rdVoid =>
            {
                var count = SceneManager.sceneCount;
                for (var i = 0; i < count; i++)
                {
                    if (SceneManager.GetSceneAt(i).isDirty)
                        return true;
                }

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
                var hasDirtyUserAssets = Resources.FindObjectsOfTypeAll<ScriptableObject>()
                    .Any(a =>
                    {
                        if (a.hideFlags.HasFlag(HideFlags.DontSaveInEditor))
                            return false;

                        if (!IsDirty(a))
                            return false;
                        
                        // I don't expect too many of those unsaved user Assets with attached ScriptableObject,
                        // so it feels safer to check them for having a real file on the disk
                        // to avoid false positives

                        var assetPath = AssetDatabase.GetAssetPath(a);
                        if (string.IsNullOrEmpty(assetPath))
                            return false;

                        return File.Exists(Path.GetFullPath(assetPath));
                    });

                if (hasDirtyUserAssets)
                    return true;


                return IsPrefabDirty();
            });
        }

        private static bool IsPrefabDirty()
        {
            // from 2018.3
            // return UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage().scene.isDirty;
            
            // from 2021.2
            // return UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage().scene.isDirty;

            try
            {
                var T = Type.GetType("UnityEditor.SceneManagement.PrefabStageUtility,UnityEditor")
                        ?? Type.GetType("UnityEditor.Experimental.SceneManagement.PrefabStageUtility,UnityEditor");
                if (T == null)
                {
                    ourLogger.Error(
                        "Types \"UnityEditor.SceneManagement.PrefabStageUtility,UnityEditor\" and \"UnityEditor.Experimental.SceneManagement.PrefabStageUtility,UnityEditor\" were not found.");
                    return false;
                }

                var getCurrentPrefabStageMethodInfo =
                    T.GetMethod("GetCurrentPrefabStage", BindingFlags.Public | BindingFlags.Static);
                if (getCurrentPrefabStageMethodInfo == null)
                {
                    ourLogger.Error("getCurrentPrefabStageMethodInfo method not found of type='{0}'", T);
                    return false;
                }

                var currentPrefabStage = getCurrentPrefabStageMethodInfo.Invoke(null, null);
                if (currentPrefabStage == null) // there is no active prefab editing
                    return false;
                
                var sceneProperty = currentPrefabStage.GetType().GetProperty("scene");
                if (sceneProperty == null)
                {
                    ourLogger.Error("'scene' prop not found in type '{0}'.", currentPrefabStage.GetType());
                    return false;
                }

                var sceneObject = sceneProperty.GetValue(currentPrefabStage, new object[]{});
                var isDirtyProperty = sceneObject.GetType().GetProperty("isDirty");
                if (isDirtyProperty == null)
                {
                    ourLogger.Error("isDirty prop not found in type '{0}'.", sceneProperty.GetType());
                    return false;
                }

                var isDirty = (bool)isDirtyProperty.GetValue(sceneObject, new object[]{});
                return isDirty;
            }
            catch (Exception e)
            {
                ourLogger.Error(e);
            }

            return false;
        }

        private static bool IsDirty(Object unityObject)
        {
#if UNITY_2019_2_OR_NEWER
            return EditorUtility.IsDirty(unityObject);
#else
            try
            {
                ourIsDirtyMethodInfo = typeof(EditorUtility).GetMethod("IsDirty",
                    BindingFlags.Static 
                    | BindingFlags.Public 
                    | BindingFlags.NonPublic, null, new[] { typeof(int)}, null);
                if (ourIsDirtyMethodInfo == null)
                {
                    ourLogger.Error("IsDirty method not found of type='{0}'", typeof(EditorUtility));
                    return false;
                }

                return (bool)ourIsDirtyMethodInfo.Invoke(null, new object[]{unityObject.GetInstanceID()});
            }
            catch (Exception e)
            {
                ourLogger.Error("Failed to invoke EditorUtility.IsDirty method with exception {0}", e);
            }
            
            return false;
#endif
        }
    }
}