using System;
using System.Reflection;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.Util.Logging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
        if (findUsagesRequest == null)
          return;

        var sceneCount = SceneManager.sceneCount;

        Debug.Log("Scene count : " + sceneCount);
        bool wasFound = false;
        for (int i = 0; i < sceneCount; i++)
        {
          var scene = SceneManager.GetSceneAt(i);          
          Debug.Log("Scene name : " + scene.name);

          if (!scene.isLoaded)
            continue;
          
          if (scene.name.Equals(findUsagesRequest.SceneName))
          {
            wasFound = true;
            SelectUsage(scene, findUsagesRequest);
            break;
          }
        }

        if (!wasFound)
        {
          if (EditorUtility.DisplayDialog("Find usages", "Do you want to load scene which contains usage?", "Ok", "No"))
          {
            var path = "Assets/Scenes/" + findUsagesRequest.SceneName + ".unity";
            Debug.Log(path);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            if (scene != null)
              SelectUsage(scene, findUsagesRequest);
          } 
        }
        
        modelValue.ShowGameObjectOnScene.SetValue(null);
      });
      

    }

    private static void SelectUsage(Scene scene, RdFindUsageRequest request)
    {
      foreach (var gameObject in scene.GetRootGameObjects())
      {
        var toSelect = TryGetGameObject(gameObject, 0, request.Path, request.LocalId);
        if (toSelect != null)
        {
          EditorGUIUtility.PingObject(toSelect);
          Selection.activeObject = toSelect;
          break;
        }
      }
    }
    
    private static GameObject TryGetGameObject(GameObject gameObject, int index,  string[] path, int localId)
    {
      Debug.Log("Look at gameobject with name:" + gameObject.name);
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

    private static bool IsLocalIdSame(GameObject go, int localId)
    {
      PropertyInfo inspectorModeInfo = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
      if (inspectorModeInfo == null)
        return false;
      
      SerializedObject serializedObject = new SerializedObject(go);
      inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);
 
      // typo in Unity
      // ReSharper disable once StringLiteralTypo
      SerializedProperty localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");
 
      Debug.Log(localId);
      Debug.Log(localIdProp.intValue);
      return localIdProp.intValue == localId;
    }
  }
}