#nullable enable
using System;
using JetBrains.Diagnostics;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters.SnapshotNavigation
{
  internal class ReflectionDataProvider
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(ReflectionDataProvider));
    internal readonly CPUProfilerModuleReflectionData? CPUProfilerModuleReflectionData;
    internal readonly ProfilerDriverReflectionData? ProfilerDriverReflectionData;
    internal readonly ProfilerFrameDataHierarchyViewReflectionData? ProfilerFrameDataHierarchyViewReflectionData;
    internal readonly ProfilerFrameDataTreeViewReflectionData? ProfilerFrameDataTreeViewReflectionData;
    internal readonly ProfilerWindowReflectionData? ProfilerWindowReflectionData;
    public readonly bool IsCompatibleWithCurrentUnityVersion;

    internal ReflectionDataProvider()
    {
      try
      {
        ProfilerDriverReflectionData = new ProfilerDriverReflectionData();
        ProfilerWindowReflectionData = new ProfilerWindowReflectionData();
        CPUProfilerModuleReflectionData = new CPUProfilerModuleReflectionData();
        ProfilerFrameDataHierarchyViewReflectionData = new ProfilerFrameDataHierarchyViewReflectionData();
        ProfilerFrameDataTreeViewReflectionData = new ProfilerFrameDataTreeViewReflectionData();

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
            $"The {nameof(ReflectionBasedAdapters.ProfilerWindowReflectionData)} is not compatible with the current Unity version.");
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