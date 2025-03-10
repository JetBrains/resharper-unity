#nullable enable
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters.SnapshotNavigation
{
  public interface ICPUProfilerModuleAdapter
  {
    IProfilerFrameDataHierarchyViewAdapter? GetFrameDataHierarchyView();
  }
}