#nullable enable
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters.SnapshotNavigation;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.UnityApiBasedAdapters
{
  public class UnityApiProfilerWindowAdapter : IProfilerWindowAdapter
  {
    private readonly IProfilerAdaptersFactory myFactory;
    private readonly ProfilerWindow? myProfilerWindow;

    public UnityApiProfilerWindowAdapter(EditorWindow? lastKnownProfilerWindow, IProfilerAdaptersFactory factory)
    {
      myFactory = factory;
      myProfilerWindow = lastKnownProfilerWindow as ProfilerWindow;
    }

    public int GetSelectedFrameIndex()
    {
      return (int)(myProfilerWindow?.selectedFrameIndex ?? -1);
    }

    public object? ProfilerWindowObject => myProfilerWindow;

    public ICPUProfilerModuleAdapter? GetCpuProfilerModule()
    {
      var cpuProfilerModule = myProfilerWindow?.GetFrameTimeViewSampleSelectionController("CPU Usage");
      if (cpuProfilerModule == null)
        return null;

      return myFactory.CreateCPUProfilerModuleAdapter(cpuProfilerModule);
    }
  }
}