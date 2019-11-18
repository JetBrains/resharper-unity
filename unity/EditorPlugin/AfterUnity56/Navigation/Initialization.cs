using System.Diagnostics;
using System.Linq;
using JetBrains.Rider.Unity.Editor.Navigation;
using JetBrains.Rider.Unity.Editor.Navigation.Window;
using JetBrains.Rider.Unity.Editor.NonUnity;
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
        if (findUsagesResult != null)
        {
          MainThreadDispatcher.Instance.Queue(() =>
          {
            ExpandMinimizedUnityWindow();
            
            EditorUtility.FocusProjectWindow();

            if (findUsagesResult.IsPrefab)
            {
              ShowUtil.ShowFileUsage(findUsagesResult.FilePath);
            }
            else
            {
              ShowUtil.ShowUsageOnScene(findUsagesResult.FilePath,  findUsagesResult.FileName, findUsagesResult.PathElements, findUsagesResult.RootIndices);
            }
          });  
        }
      });
      
      modelValue.FindUsageResults.Advise(connectionLifetime, result =>
      {
        if (result != null)
        {
          MainThreadDispatcher.Instance.Queue(() =>
          {
            GUI.BringWindowToFront(EditorWindow.GetWindow<SceneView>().GetInstanceID());
            GUI.BringWindowToFront(EditorWindow.GetWindow(typeof(SceneView).Assembly.GetType("UnityEditor.SceneHierarchyWindow")).GetInstanceID());      
            GUI.BringWindowToFront(EditorWindow.GetWindow(typeof(SceneView).Assembly.GetType("UnityEditor.ProjectBrowser")).GetInstanceID());

            var window = FindUsagesWindow.GetWindow(result.Target);
            window.SetDataToEditor(result.Elements);
          });  
        }
      });
      
      modelValue.ShowFileInUnity.Advise(connectionLifetime, result =>
      {
        if (result != null)
        {
          MainThreadDispatcher.Instance.Queue(() =>
          {
            EditorUtility.FocusProjectWindow();
            ShowUtil.ShowFileUsage(result);
          });  
        }
      });
    }

    private static void ExpandMinimizedUnityWindow()
    {
      if (PluginSettings.SystemInfoRiderPlugin.operatingSystemFamily == OperatingSystemFamilyRider.Windows)
      {
        var topLevelWindows = User32Dll.GetTopLevelWindowHandles();
        var windowHandles = topLevelWindows
          .Where(hwnd => User32Dll.GetWindowProcessId(hwnd) == Process.GetCurrentProcess().Id).ToArray();

        foreach (var windowHandle in windowHandles)
        {
          if (User32Dll.IsIconic(windowHandle))
          {
            User32Dll.ShowWindow(windowHandle, 9);
            User32Dll.SetForegroundWindow(windowHandle);
          }
        }
      }
    }
  }
}