using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Rider.Unity.Editor.FindUsages.Window;
using JetBrains.Rider.Unity.Editor.NonUnity;
using UnityEditor;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.FindUsages
{
  internal static class FindUsagesModelHelper
  {
    private static readonly ILog ourLogger = Log.GetLog("Navigation.Initialization");

    public static void Advise(Lifetime modelLifetime, BackendUnityModel model)
    {
      model.ShowUsagesInUnity.Advise(modelLifetime, findUsagesResult =>
      {
        if (findUsagesResult == null) return;

        MainThreadDispatcher.AssertThread();

        ExpandMinimizedUnityWindow();

        EditorUtility.FocusProjectWindow();
        switch (findUsagesResult)
        {
          case AnimatorFindUsagesResult animatorUsage:
            ShowUtil.ShowAnimatorUsage(animatorUsage.PathElements, animatorUsage.FilePath);
            return;
          case HierarchyFindUsagesResult _
            when findUsagesResult.Extension.Equals(".prefab", StringComparison.OrdinalIgnoreCase):
            ShowUtil.ShowFileUsage(findUsagesResult.FilePath);
            break;
          case HierarchyFindUsagesResult hierarchyFindUsagesResult:
            ShowUtil.ShowUsageOnScene(findUsagesResult.FilePath, findUsagesResult.FileName,
              hierarchyFindUsagesResult.PathElements, hierarchyFindUsagesResult.RootIndices);
            break;
          case AnimationFindUsagesResult animationEventUsage:
            ShowUtil.ShowAnimationEventUsage(animationEventUsage.FilePath);
            break;
          default:
            ShowUtil.ShowFileUsage(findUsagesResult.FilePath);
            break;
        }
      });

      model.SendFindUsagesSessionResult.Advise(modelLifetime, result =>
      {
        MainThreadDispatcher.AssertThread();

        if (result != null)
        {
#if UNITY_CORCLR_OR_NEWER
          GUI.BringWindowToFront((long)(ulong)EditorWindow.GetWindow<SceneView>().GetInstanceID());
          GUI.BringWindowToFront((long)(ulong)EditorWindow
            .GetWindow(typeof(SceneView).Assembly.GetType("UnityEditor.SceneHierarchyWindow")).GetInstanceID());
          GUI.BringWindowToFront((long)(ulong)EditorWindow
            .GetWindow(typeof(SceneView).Assembly.GetType("UnityEditor.ProjectBrowser")).GetInstanceID());     
#else 
          GUI.BringWindowToFront(EditorWindow.GetWindow<SceneView>().GetInstanceID());
          GUI.BringWindowToFront(EditorWindow
            .GetWindow(typeof(SceneView).Assembly.GetType("UnityEditor.SceneHierarchyWindow")).GetInstanceID());
          GUI.BringWindowToFront(EditorWindow
            .GetWindow(typeof(SceneView).Assembly.GetType("UnityEditor.ProjectBrowser")).GetInstanceID());
#endif
          var window = FindUsagesWindow.GetWindow(result.Target);
          window.SetDataToEditor(result.Elements);
        }
      });

      model.ShowFileInUnity.AdviseNotNull(modelLifetime, result =>
      {
        var fullName = new FileInfo(result).FullName;
        // only works for Assets folder
        var matchedUnityPath = fullName.Substring(Directory.GetParent(Application.dataPath).FullName.Length + 1);

        var asset = AssetDatabase.LoadAssetAtPath(matchedUnityPath, typeof(UnityEngine.Object));
        if (asset == null)
        {
          // works for any assets including local packages, but might be slow on big projects
          matchedUnityPath = AssetDatabase.GetAllAssetPaths()
            .FirstOrDefault(a =>
              new FileInfo(Path.GetFullPath(a)).FullName ==
              fullName); // FileInfo normalizes separators (required on Windows)
          if (matchedUnityPath != null)
            asset = AssetDatabase.LoadAssetAtPath(matchedUnityPath, typeof(UnityEngine.Object));
        }

        if (asset != null)
        {
          ExpandMinimizedUnityWindow();
          EditorUtility.FocusProjectWindow();
          ShowUtil.ShowObjectUsage(asset);
        }
        else
        {
          ourLogger.Warn($"ShowFileInUnity attempt failed. No asset matched for path {result}");
        }
      });
    }

    private static void ExpandMinimizedUnityWindow()
    {
      if (PluginSettings.SystemInfoRiderPlugin.OS == PathLocator.OS.Windows)
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