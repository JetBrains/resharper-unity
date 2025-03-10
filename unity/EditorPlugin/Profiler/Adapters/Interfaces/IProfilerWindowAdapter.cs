#nullable enable
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters.SnapshotNavigation;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces
{
  public interface IProfilerWindowAdapter
  {
    object? ProfilerWindowObject { get; }
    int GetSelectedFrameIndex();
    ICPUProfilerModuleAdapter? GetCpuProfilerModule();
  }
}