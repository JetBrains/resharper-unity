using JetBrains.Diagnostics;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.SnapshotNavigation
{
  internal class ProfilerFrameDataTreeViewAdapter
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(ProfilerFrameDataTreeViewAdapter));
    private readonly ProfilerFrameDataTreeViewReflectionData myReflectionData;
    private readonly object myTreeView;

    private ProfilerFrameDataTreeViewAdapter(object treeView,
      ProfilerFrameDataTreeViewReflectionData reflectionData)
    {
      myTreeView = treeView;
      myReflectionData = reflectionData;
    }

    public static ProfilerFrameDataTreeViewAdapter Create(object treeView,
      ProfilerFrameDataTreeViewReflectionData reflectionData)
    {
      if (!reflectionData.IsValid())
      {
        ourLogger.Verbose($"{reflectionData.GetType().Name} is not valid.");
        return null;
      }

      if (treeView?.GetType() != reflectionData.ProfilerFrameDataTreeViewType)
      {
        ourLogger.Verbose(
          $"Type '{ProfilerFrameDataTreeViewReflectionData.ProfilerFrameDataTreeViewTypeName}' expected.");
        return null;
      }

      return new ProfilerFrameDataTreeViewAdapter(treeView, reflectionData);
    }

    public TreeViewControllerAdapter GetTreeViewController(TreeViewControllerReflectionData treeViewControllerReflectionData)
    {
      return TreeViewControllerAdapter.Create(myReflectionData.TreeViewControllerFieldInfo.GetValue(myTreeView),
        treeViewControllerReflectionData);
    }
  }
}