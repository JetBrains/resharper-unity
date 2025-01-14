using JetBrains.Annotations;
using JetBrains.Diagnostics;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.SnapshotNavigation
{
  internal class ProfilerFrameDataHierarchyViewAdapter
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(ProfilerFrameDataHierarchyViewAdapter));

    private readonly object myHierarchyView;
    private readonly ProfilerFrameDataHierarchyViewReflectionData myReflectionData;

    private ProfilerFrameDataHierarchyViewAdapter(object hierarchyView,
      ProfilerFrameDataHierarchyViewReflectionData reflectionData)
    {
      myHierarchyView = hierarchyView;
      myReflectionData = reflectionData;
    }

    public static ProfilerFrameDataHierarchyViewAdapter Create(object hierarchyView,
      ProfilerFrameDataHierarchyViewReflectionData reflectionData)
    {
      if (!reflectionData.IsValid())
      {
        ourLogger.Verbose($"{reflectionData.GetType().Name} is not valid.");
        return null;
      }

      if (hierarchyView?.GetType() != reflectionData.ProfilerFrameDataHierarchyViewType)
      {
        ourLogger.Verbose(
          $"Type '{ProfilerFrameDataHierarchyViewReflectionData.FrameDataHierarchyViewTypeName}' expected.");
        return null;
      }

      return new ProfilerFrameDataHierarchyViewAdapter(hierarchyView, reflectionData);
    }


    //It is null until it is opened once 
    [CanBeNull]
    public ProfilerFrameDataTreeViewAdapter GetTreeView(ProfilerFrameDataTreeViewReflectionData profilerFrameDataTreeViewReflectionData)
    {
      var treeView = myReflectionData.TreeViewFieldInfo.GetValue(myHierarchyView);

      if (treeView == null)
        return null;

      return ProfilerFrameDataTreeViewAdapter.Create(treeView,
        profilerFrameDataTreeViewReflectionData);
    }

    public void InitIfNeeded()
    {
      myReflectionData.InitIfNeededMethodInfo.Invoke(myHierarchyView, null);
    }
  }
}