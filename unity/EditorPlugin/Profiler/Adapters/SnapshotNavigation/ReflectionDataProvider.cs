using System;
using JetBrains.Diagnostics;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.SnapshotNavigation
{
  internal class ReflectionDataProvider
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(ReflectionDataProvider));
    public readonly bool IsCompatibleWithCurrentUnityVersion;
    internal readonly ProfilerDriverReflectionData ProfilerDriverReflectionData;
    internal readonly ProfilerWindowReflectionData ProfilerWindowReflectionData;
    internal readonly CPUProfilerModuleReflectionData CPUProfilerModuleReflectionData;
    internal readonly ProfilerFrameDataHierarchyViewReflectionData ProfilerFrameDataHierarchyViewReflectionData;
    internal readonly ProfilerFrameDataTreeViewReflectionData ProfilerFrameDataTreeViewReflectionData;
    internal readonly TreeViewControllerReflectionData TreeViewControllerReflectionData;

    internal ReflectionDataProvider()
    {
      try
      {
        ProfilerDriverReflectionData = new ProfilerDriverReflectionData();
        ProfilerWindowReflectionData = new ProfilerWindowReflectionData();
        CPUProfilerModuleReflectionData = new CPUProfilerModuleReflectionData();
        ProfilerFrameDataHierarchyViewReflectionData = new ProfilerFrameDataHierarchyViewReflectionData();
        ProfilerFrameDataTreeViewReflectionData = new ProfilerFrameDataTreeViewReflectionData();
        TreeViewControllerReflectionData = new TreeViewControllerReflectionData();

        if (!ProfilerDriverReflectionData.IsValid())
        {
          IsCompatibleWithCurrentUnityVersion = false;
          ourLogger.Verbose(
            $"The {nameof(SnapshotNavigation.ProfilerDriverReflectionData)} is not compatible with the current Unity version.");
          return;
        }

        if (!ProfilerWindowReflectionData.IsValid())
        {
          IsCompatibleWithCurrentUnityVersion = false;
          ourLogger.Verbose(
            $"The {nameof(SnapshotNavigation.ProfilerWindowReflectionData)} is not compatible with the current Unity version.");
          return;
        }

        if (!CPUProfilerModuleReflectionData.IsValid())
        {
          IsCompatibleWithCurrentUnityVersion = false;
          ourLogger.Verbose(
            $"The {nameof(SnapshotNavigation.CPUProfilerModuleReflectionData)} is not compatible with the current Unity version.");
          return;
        }

        if (!ProfilerFrameDataHierarchyViewReflectionData.IsValid())
        {
          IsCompatibleWithCurrentUnityVersion = false;
          ourLogger.Verbose(
            $"The {nameof(SnapshotNavigation.ProfilerFrameDataHierarchyViewReflectionData)} is not compatible with the current Unity version.");
          return;
        }

        if (!ProfilerFrameDataTreeViewReflectionData.IsValid())
        {
          IsCompatibleWithCurrentUnityVersion = false;
          ourLogger.Verbose(
            $"The {nameof(SnapshotNavigation.ProfilerFrameDataTreeViewReflectionData)} is not compatible with the current Unity version.");
          return;
        }

        if (!TreeViewControllerReflectionData.IsValid())
        {
          IsCompatibleWithCurrentUnityVersion = false;
          ourLogger.Verbose(
            $"The {nameof(SnapshotNavigation.TreeViewControllerReflectionData)} is not compatible with the current Unity version.");
          return;
        }

        IsCompatibleWithCurrentUnityVersion = true;
      }
      catch (Exception e)
      {
        IsCompatibleWithCurrentUnityVersion = false;
        ourLogger.Verbose($"Exception while initializing {nameof(ReflectionDataProvider)}:  {e}");
      }
    }
  }
}