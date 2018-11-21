using System;
using System.Linq;
using System.Reflection;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.Util.Logging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace JetBrains.Rider.Unity.Editor.Navigation
{
  [InitializeOnLoad]
  public static class EntryPoint
  {
    private static readonly ILog ourLogger = Log.GetLog("UnitTesting.EntryPoint");
    
    static EntryPoint()
    {
      if (!PluginEntryPoint.Enabled)
        return;
      ourLogger.Verbose("UnitTesting.EntryPoint");

      PluginEntryPoint.OnModelInitialization += OnModelInitializationHandler;
      AppDomain.CurrentDomain.DomainUnload += (EventHandler) ((_, __) =>
      {
        PluginEntryPoint.OnModelInitialization -= OnModelInitializationHandler;
      });
    }
    
    
    private static void OnModelInitializationHandler(UnityModelAndLifetime modelAndLifetime)
    {
      var modelValue = modelAndLifetime.Model;
      var connectionLifetime = modelAndLifetime.Lifetime;
      modelValue.ShowGameObjectOnScene.View(connectionLifetime, (lt, findUsagesRequest) =>
      {
        MainThreadDispatcher.Instance.Queue(() =>
        {
          if (lt.IsTerminated)
            return;

          if (findUsagesRequest == null)
            return;

          if (findUsagesRequest is RdFindUsageRequestScene requestScene)
            ShowUsageOnScene(requestScene.FilePath, requestScene.PathElements, requestScene.LocalId);

          if (findUsagesRequest is RdFindUsageRequestPrefab requestPrefab)
          {
            ShowPrefabUsage(requestPrefab.FilePath, requestPrefab.PathElements);
          }
          
          modelValue.ShowGameObjectOnScene.SetValue(null);
        });
      });
      
      modelValue.FindUsageResult.View(connectionLifetime, (lt, result) =>
      {
        MainThreadDispatcher.Instance.Queue(() =>
        {
          if (result == null)
            return;
          
          if (lt.IsTerminated)
            return;
          
          var window = EditorWindow.GetWindow<FindUsagesWindow>();
          window.SetDataToEditor(result);
          window.titleContent = new GUIContent ("Find usages");
          modelValue.FindUsageResult.SetValue(null);
        });
      });
    }

    public static void ShowPrefabUsage(string filePath, string[] path)
    {
      EditorUtility.FocusProjectWindow();
      var prefab = AssetDatabase.LoadAssetAtPath(filePath + ".prefab", typeof(GameObject));
      Selection.activeObject = prefab;
      EditorGUIUtility.PingObject(prefab);
    }
    
    public static void ShowUsageOnScene(string filePath, string[] path, string localId)
    {
      var sceneName = filePath.Split('/').Last();
      var sceneCount = SceneManager.sceneCount;

      bool wasFound = false;
      for (int i = 0; i < sceneCount; i++)
      {
        var scene = SceneManager.GetSceneAt(i);

        if (!scene.isLoaded)
          continue;

        if (scene.name.Equals(sceneName))
        {
          wasFound = true;
          SelectUsageOnScene(scene, path, localId);
          break;
        }
      }

      if (!wasFound)
      {
        if (EditorUtility.DisplayDialog("Find usages", "Do you want to load scene which contains usage?", "Ok", "No"))
        {
          var scene = EditorSceneManager.OpenScene(filePath + ".unity", OpenSceneMode.Additive);
          SelectUsageOnScene(scene, path, localId, true);
        }
      }
    } 
    
    
    private static void SelectUsageOnScene(Scene scene, string[] path, string localId, bool doNotMoveCamera = false) // When scene is loaded, each GO has default transform
    {
      foreach (var gameObject in scene.GetRootGameObjects())
      {
        var toSelect = TryGetGameObject(gameObject, 0, path, localId);
        if (toSelect != null)
        {
          GUI.BringWindowToFront(EditorWindow.GetWindow<SceneView>().GetInstanceID());
          GUI.BringWindowToFront(EditorWindow.GetWindow(typeof(SceneView).Assembly.GetType("UnityEditor.SceneHierarchyWindow")).GetInstanceID());
          EditorGUIUtility.PingObject(toSelect);
          Selection.activeObject = toSelect;
          if (!doNotMoveCamera) 
           SceneView.lastActiveSceneView.FrameSelected();
          break;
        }
      }
    }
    
    private static GameObject TryGetGameObject(GameObject gameObject, int index,  string[] path, string localId)
    {
      if (index >= path.Length)
        return null;

      if (!gameObject.name.Equals(path[index]))
        return null;

      if (index + 1 == path.Length && IsLocalIdSame(gameObject, localId))
        return gameObject;

      var childCount = gameObject.transform.childCount;

      for (int i = 0; i < childCount; i++)
      {
         var result = TryGetGameObject(gameObject.transform.GetChild(i).gameObject, index + 1, path, localId);
         if (result != null)
           return result;
      }

      return null;
    }

    private static bool IsLocalIdSame(GameObject go, string localId)
    {
      PropertyInfo inspectorModeInfo = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
      if (inspectorModeInfo == null)
        return false;
      
      SerializedObject serializedObject = new SerializedObject(go);
      inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);
 
      // typo in Unity
      // ReSharper disable once StringLiteralTypo
      SerializedProperty localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");
 
      return localIdProp.intValue == int.Parse(localId);
    }
  }
}