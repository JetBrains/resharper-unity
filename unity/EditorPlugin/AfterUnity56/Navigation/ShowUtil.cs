using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Rider.Model.Unity.BackendUnity;
using UnityEditor;
using UnityEditor.Animations;
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

    public static void ShowAnimatorUsage([NotNull] string[] pathElements, [NotNull] string controllerFilePath)
    {
        var parent = FindParentForElementToSelect(pathElements, controllerFilePath);
        if (parent is null) return;
        var lastName = pathElements.Last();
        if (lastName is null) return;
        var elementToSelect = pathElements.Length != 1 ? FindChildElementToSelect(parent, lastName) : parent;
        EditorGUIUtility.PingObject(elementToSelect);
        Selection.activeObject = elementToSelect;
    }

    [CanBeNull]
    private static AnimatorStateMachine FindParentForElementToSelect([NotNull] IList<string> pathElements,
                                                                     [NotNull] string controllerFilePath)
    {
        var asset = AssetDatabase.LoadAssetAtPath(controllerFilePath, typeof(Object));
        if (!(asset is AnimatorController controller)) return null;
        var currentStateMachine = FindLayerStateMachine(pathElements, controller);
        if (currentStateMachine is null) return null;
        for (int i = 1, parentElementsCount = pathElements.Count - 1; i < parentElementsCount; i++)
        {
            var name = pathElements[i];
            if (name is null) return null;
            currentStateMachine = FindChildStateMachine(currentStateMachine, name);
            if (currentStateMachine is null) return null;
        }
        return currentStateMachine;
    }

    [CanBeNull]
    private static AnimatorStateMachine FindLayerStateMachine([NotNull] IList<string> pathElements, 
                                                              [NotNull] AnimatorController controller)
    {
        var layers = controller.layers;
        if (layers is null) return null;
        var layerName = pathElements[0];
        if (layerName is null) return null;
        var animatorControllerLayer = layers
            .Where(l => l != null && layerName.Equals(l.name))
            .Select(layer => layer)
            .FirstOrDefault();
        return animatorControllerLayer?.stateMachine;
    }

    private static Object FindChildElementToSelect([NotNull] AnimatorStateMachine currentStateMachine, 
                                                   [NotNull] string name)
    {
        Object toSelect;
        var childStateMachine = FindChildStateMachine(currentStateMachine, name);
        if (!(childStateMachine is null)) toSelect = childStateMachine;
        else toSelect = FindChildState(currentStateMachine, name);
        return toSelect;
    }

    [CanBeNull]
    private static AnimatorStateMachine FindChildStateMachine([NotNull] AnimatorStateMachine animatorStateMachine,
                                                              [NotNull] string name)
    {
        return animatorStateMachine.stateMachines?
            .Select(stateMachine => stateMachine.stateMachine)
            .FirstOrDefault(stateMachine => stateMachine != null && name.Equals(stateMachine.name));
    }

    [CanBeNull]
    private static AnimatorState FindChildState([NotNull] AnimatorStateMachine stateMachine, [NotNull] string name)
    {
        return stateMachine.states?
            .Select(state => state.state)
            .FirstOrDefault(state => state != null && name.Equals(state.name));
    }
  }
}