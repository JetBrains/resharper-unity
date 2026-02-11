#nullable enable
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters.SnapshotNavigation;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces
{
  public interface IProfilerWindowAdapter
  {
    object? ProfilerWindowObject { get; }
    int GetSelectedFrameIndex();
    void SetSelectedFrameIndex(int frameIndex);
    ICPUProfilerModuleAdapter? GetCpuProfilerModule();
    void SetSelectedThread(int threadId);
    int GetSelectedThreadId();
  }
}