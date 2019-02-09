using JetBrains.Rider.Unity.Editor.Navigation;
using JetBrains.Rider.Unity.Editor.Navigation.Window;
using UnityEditor;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.AfterUnity56.Navigation
{
  public static class Initialization
  {    
    public static void OnModelInitializationHandler(UnityModelAndLifetime modelAndLifetime)
    {
      var modelValue = modelAndLifetime.Model;
      var connectionLifetime = modelAndLifetime.Lifetime;
      modelValue.ShowGameObjectOnScene.Advise(connectionLifetime,  findUsagesResult =>
      {
        if (findUsagesResult == null)
          return;
        
        MainThreadDispatcher.Instance.Queue(() =>
        {
          if (findUsagesResult.IsPrefab)
          {
            ShowUtil.ShowPrefabUsage(findUsagesResult.FilePath, findUsagesResult.PathElements);
          }
          else
          {
            ShowUtil.ShowUsageOnScene(findUsagesResult.FilePath,  findUsagesResult.FileName, findUsagesResult.PathElements, findUsagesResult.RootIndices);
          }
        });
      });
      
      modelValue.FindUsageResults.Advise(connectionLifetime, result =>
      {
        if (result == null)
          return;

        MainThreadDispatcher.Instance.Queue(() =>
        {
          GUI.BringWindowToFront(EditorWindow.GetWindow<SceneView>().GetInstanceID());
          GUI.BringWindowToFront(EditorWindow.GetWindow(typeof(SceneView).Assembly.GetType("UnityEditor.SceneHierarchyWindow")).GetInstanceID());      
          GUI.BringWindowToFront(EditorWindow.GetWindow(typeof(SceneView).Assembly.GetType("UnityEditor.ProjectBrowser")).GetInstanceID());

          var window = FindUsagesWindow.GetWindow(result.Target);
          window.SetDataToEditor(result.Elements);
        });
      });
    }
  }
}