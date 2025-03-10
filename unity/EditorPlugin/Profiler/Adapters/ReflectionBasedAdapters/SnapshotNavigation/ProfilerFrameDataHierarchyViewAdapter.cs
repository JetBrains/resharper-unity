#nullable enable
using JetBrains.Diagnostics;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters.SnapshotNavigation
{
  internal class ProfilerFrameDataHierarchyViewAdapter : IProfilerFrameDataHierarchyViewAdapter
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(ProfilerFrameDataHierarchyViewAdapter));
    private readonly AbstractAdaptersFactory myFactory;
    private readonly object myHierarchyView;
    private readonly ProfilerFrameDataHierarchyViewReflectionData? myReflectionData;

    internal ProfilerFrameDataHierarchyViewAdapter(object hierarchyView,
      ProfilerFrameDataHierarchyViewReflectionData? reflectionData, AbstractAdaptersFactory factory)
    {
      myHierarchyView = hierarchyView;
      myReflectionData = reflectionData;
      myFactory = factory;
    }

    public void InitIfNeeded()
    {
      if (myReflectionData?.InitIfNeededMethodInfo == null)
      {
        ourLogger.Verbose(
          $"Can't call {nameof(InitIfNeeded)}: {nameof(myReflectionData.InitIfNeededMethodInfo)} is null.");
        return;
      }

      myReflectionData.InitIfNeededMethodInfo.Invoke(myHierarchyView, null);
    }

    //It is null until it is opened once 
    public IProfilerFrameDataTreeViewAdapter? GetTreeView()
    {
      if (myReflectionData == null)
      {
        ourLogger.Verbose($"Can't get {nameof(GetTreeView)}: {nameof(myReflectionData)} is null.");
        return null;
      }

      if (myReflectionData.TreeViewFieldInfo == null)
      {
        ourLogger.Verbose($"Can't get {nameof(GetTreeView)}: {nameof(myReflectionData.TreeViewFieldInfo)} is null.");
        return null;
      }

      var treeView = myReflectionData.TreeViewFieldInfo.GetValue(myHierarchyView);

      if (treeView == null)
        return null;

      return myFactory.CreateProfilerFrameDataTreeViewAdapter(treeView);
    }
  }
}