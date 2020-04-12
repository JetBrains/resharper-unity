using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JetBrains.Rider.Unity.Editor.Navigation
{
  internal static class ShowUtil
  {
    public static void ShowFileUsage(string filePath)
    {
      var prefab = AssetDatabase.LoadAssetAtPath(filePath, typeof(Object));
      Selection.activeObject = prefab;
      EditorGUIUtility.PingObject(prefab);
    }
    
    public static void ShowUsageOnScene(string filePath, string sceneName, string[] path, int[] rootIndices)
    {
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
          SelectUsageOnScene(scene, path, rootIndices);
          break;
        }
      }

      if (!wasFound)
      {
        var result = EditorUtility.DisplayDialogComplex("Find usages",
          $"Do you want to close the current scene and open scene \"{sceneName}.unity\"?",
          "Open Scene", 
          "Cancel",
          "Open Scene Additive");

        switch (result)
        {
          case 0:
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            var scene = EditorSceneManager.OpenScene(filePath, OpenSceneMode.Single);
            SelectUsageOnScene(scene, path, rootIndices, true);
            break;
          case 1:
            return;
          case 2:
            var sceneAdditive = EditorSceneManager.OpenScene(filePath, OpenSceneMode.Additive);
            SelectUsageOnScene(sceneAdditive, path, rootIndices, true);
            break;
        }
      }
    }

    private static void SelectUsageOnScene(Scene scene, string[] path, int[] rootIndices, bool doNotMoveCamera = false)
    {
      if (rootIndices.Length == 0)
        return;

      var rootObjects = scene.GetRootGameObjects();

      if (rootIndices[0] >= rootObjects.Length)
        return;

      GameObject toSelect = rootObjects[rootIndices[0]];
      if (!toSelect.name.Equals(path[0]))
        return;

      for (int i = 1; i < rootIndices.Length; i++)
      {
        var transform = toSelect.transform;
        if (rootIndices[i] >= transform.childCount || rootIndices[i] < 0)
          return;

        toSelect = transform.GetChild(rootIndices[i]).gameObject;
        if (!toSelect.name.Equals(path[i])) // check that object is same (with false-positives, but we warn user if scene is dirty)
          return;
      }

      // Bring scene hierarchy and scene view to front
      EditorGUIUtility.PingObject(toSelect);
      Selection.activeObject = toSelect;
      if (!doNotMoveCamera) // When scene is loaded, each GO has default transform
        SceneView.lastActiveSceneView.FrameSelected();
    }
  }
}