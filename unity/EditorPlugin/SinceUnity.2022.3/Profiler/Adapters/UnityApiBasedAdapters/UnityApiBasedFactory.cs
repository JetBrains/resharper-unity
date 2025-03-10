#nullable enable
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters;
using JetBrains.Rider.Unity.Editor.Profiler.SnapshotNavigation;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.UnityApiBasedAdapters
{
  internal class UnityApiBasedFactory : AbstractAdaptersFactory
  {
    public override IProfilerWindowSelectionDataProvider? CreateProfilerWindowFacade()
    {
      return new ProfilerWindowFacade(MyReflectionDataProvider, this);
    }

    public override IProfilerSnapshotDriverAdapter? CreateProfilerSnapshotDriverAdapter()
    {
      return new UnityApiProfilerSnapshotDriverAdapter();
    }

    public override IProfilerWindowAdapter? CreateProfilerWindowAdapter(EditorWindow? lastKnownProfilerWindow)
    {
      return new UnityApiProfilerWindowAdapter(lastKnownProfilerWindow, this);
    }

    public override IProfilerWindowTypeChecker? CreateProfilerWindowTypeChecker()
    {
      return new UnityApiProfilerWindowChecker();
    }
  }
}