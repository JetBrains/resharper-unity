#nullable enable
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters.SnapshotNavigation;
using JetBrains.Rider.Unity.Editor.Profiler.SnapshotNavigation;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces
{
  public interface IProfilerAdaptersFactory
  {
    IProfilerWindowSelectionDataProvider? CreateProfilerWindowFacade();
    IProfilerSnapshotDriverAdapter? CreateProfilerSnapshotDriverAdapter();
    IProfilerWindowAdapter? CreateProfilerWindowAdapter(EditorWindow? lastKnownProfilerWindow);
    IProfilerWindowTypeChecker? CreateProfilerWindowTypeChecker();
    ITreeViewControllerAdapter? TryCreateTreeViewControllerAdapter(EditorWindow profilerWindow);
    ICPUProfilerModuleAdapter? CreateCPUProfilerModuleAdapter(object module);
  }
}