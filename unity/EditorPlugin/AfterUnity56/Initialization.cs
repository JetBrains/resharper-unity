using System;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Rider.Unity.Editor.AfterUnity56.UnitTesting;
using JetBrains.Rider.Unity.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JetBrains.Rider.Unity.Editor.AfterUnity56
{
    public static class Initialization
    {
        private static readonly ILog ourLogger = Log.GetLog("Initialization");
#if !UNITY_2019_2_OR_NEWER
        private static MethodInfo ourIsDirtyMethodInfo;
#endif

        public static void Advise(Lifetime modelLifetime, BackendUnityModel model)
        {
            ourLogger.Verbose("AdviseUnitTestLaunch");

            model.GetCompilationResult.Set(_ => !EditorUtility.scriptCompilationFailed);
            model.UnitTestLaunch.Advise(modelLifetime, launch =>
            {
                new TestEventsSender(launch);
                UnityEditorTestLauncher.SupportAbortNew(launch); // TestFramework 1.2.x
            });

            model.RunUnitTestLaunch.Set(rdVoid =>
            {
                if (!model.UnitTestLaunch.HasValue()) return false;
                if (EditorApplication.isPlaying)
                    throw new InvalidOperationException("Running tests during the Play mode is not possible.");
                var testLauncher = new UnityEditorTestLauncher(model.UnitTestLaunch.Value, modelLifetime);
                return testLauncher.TryLaunchUnitTests();
            });

            AdviseOnHasUnsavedChanges(model);
        }

        private static void AdviseOnHasUnsavedChanges(BackendUnityModel model)
        {
            model.HasUnsavedState.Set(rdVoid =>
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
            /* For testing:
             1. create a prefab
             2. open it for editing
             3. uncheck "Auto Save"
             4. make any change to the prefab
            */

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

        private static bool IsDirty(UnityEngine.Object unityObject)
        {
            // EditorUtility.IsDirty is internal until 2019.1
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