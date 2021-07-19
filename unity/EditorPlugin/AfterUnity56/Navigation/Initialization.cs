using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.Collections.Viewable;
using JetBrains.Rider.Model.Unity.BackendUnity;
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
      modelValue.ShowUsagesInUnity.Advise(connectionLifetime, findUsagesResult =>
      {
        if (findUsagesResult != null)
        {
          MainThreadDispatcher.Instance.Queue(() => // todo: remove MainThreadDispatcher call - not needed
          {
            ExpandMinimizedUnityWindow();

            EditorUtility.FocusProjectWindow();
            switch (findUsagesResult)
            {
                case AnimatorFindUsagesResult animatorUsage:
                    ShowUtil.ShowAnimatorUsage(animatorUsage.PathElements, animatorUsage.FilePath);
                    return;
                case HierarchyFindUsagesResult _ when findUsagesResult.Extension.Equals(".prefab", StringComparison.OrdinalIgnoreCase):
                    ShowUtil.ShowFileUsage(findUsagesResult.FilePath);
                    break;
                case HierarchyFindUsagesResult hierarchyFindUsagesResult:
                    ShowUtil.ShowUsageOnScene(findUsagesResult.FilePath,  findUsagesResult.FileName, hierarchyFindUsagesResult.PathElements, hierarchyFindUsagesResult.RootIndices);
                    break;
                case AnimationFindUsagesResult animationEventUsage:
                    ShowUtil.ShowAnimationEventUsage(animationEventUsage.FilePath);
                    break;
                default:
                    ShowUtil.ShowFileUsage(findUsagesResult.FilePath);
                    break;
            }
          });
        }
      });

      modelValue.SendFindUsagesSessionResult.Advise(connectionLifetime, result =>
      {
        if (result != null)
        {
          MainThreadDispatcher.Instance.Queue(() => // todo: remove MainThreadDispatcher call - not needed
          {
            GUI.BringWindowToFront(EditorWindow.GetWindow<SceneView>().GetInstanceID());
            GUI.BringWindowToFront(EditorWindow.GetWindow(typeof(SceneView).Assembly.GetType("UnityEditor.SceneHierarchyWindow")).GetInstanceID());
            GUI.BringWindowToFront(EditorWindow.GetWindow(typeof(SceneView).Assembly.GetType("UnityEditor.ProjectBrowser")).GetInstanceID());

            var window = FindUsagesWindow.GetWindow(result.Target);
            window.SetDataToEditor(result.Elements);
          });
        }
      });

      modelValue.ShowFileInUnity.AdviseNotNull(connectionLifetime, result =>
      {
        var fullName = new FileInfo(result).FullName;
        // only works for Assets folder
        var matchedUnityPath = fullName.Substring(Directory.GetParent(Application.dataPath).FullName.Length + 1);

        if (fullName != new FileInfo(Path.GetFullPath(matchedUnityPath)).FullName)
        {
          // works for any assets including local packages, but might be slow on big projects
          matchedUnityPath = AssetDatabase.GetAllAssetPaths()
            .FirstOrDefault(a =>
              new FileInfo(Path.GetFullPath(a)).FullName ==
              fullName); // FileInfo normalizes separators (required on Windows)
        }

        if (matchedUnityPath != null)
        {
          ExpandMinimizedUnityWindow();
          EditorUtility.FocusProjectWindow();
          ShowUtil.ShowFileUsage(matchedUnityPath);
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