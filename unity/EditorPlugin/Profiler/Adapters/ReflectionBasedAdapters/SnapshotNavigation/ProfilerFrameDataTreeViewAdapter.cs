#nullable enable
using JetBrains.Diagnostics;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters.SnapshotNavigation
{
  internal class ProfilerFrameDataTreeViewAdapter : IProfilerFrameDataTreeViewAdapter
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(ProfilerFrameDataTreeViewAdapter));
    private readonly AbstractAdaptersFactory myReflectionBasedAdaptersFactory;
    private readonly ProfilerFrameDataTreeViewReflectionData? myReflectionData;
    private readonly object? myTreeView;

    internal ProfilerFrameDataTreeViewAdapter(object? treeView, ProfilerFrameDataTreeViewReflectionData? reflectionData,
      AbstractAdaptersFactory reflectionBasedAdaptersFactory)
    {
      myTreeView = treeView;
      myReflectionData = reflectionData;
      myReflectionBasedAdaptersFactory = reflectionBasedAdaptersFactory;
    }

    public ITreeViewControllerAdapter? GetTreeViewController()
    {
      if (myReflectionData == null)
      {
        ourLogger.Verbose($"Can't get {nameof(GetTreeViewController)}: {nameof(myReflectionData)} is null.");
        return null;
      }

      if (myReflectionData.TreeViewControllerFieldInfo == null)
      {
        ourLogger.Verbose(
          $"Can't get {nameof(GetTreeViewController)}: {nameof(myReflectionData.TreeViewControllerFieldInfo)} is null.");
        return null;
      }

      var treeViewController = myReflectionData.TreeViewControllerFieldInfo.GetValue(myTreeView);

      return myReflectionBasedAdaptersFactory.CreateTreeViewControllerAdapter(treeViewController);
    }
  }
}