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
      var prefab = AssetDatabase.LoadAssetAtPath(filePath, typeof(GameObject));
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
        if (EditorUtility.DisplayDialog("Find usages", "Do you want to load scene which contains usage?", "Ok", "No"))
        {
          var scene = EditorSceneManager.OpenScene(filePath, OpenSceneMode.Additive);
          SelectUsageOnScene(scene, path, rootIndices, true);
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