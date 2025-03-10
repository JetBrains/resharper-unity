#nullable enable
using JetBrains.Diagnostics;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters.SnapshotNavigation
{
  internal class CPUProfilerModuleAdapter : ICPUProfilerModuleAdapter
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(CPUProfilerModuleAdapter));
    private readonly AbstractAdaptersFactory myAdaptersFactory;
    private readonly object myModule;
    private readonly CPUProfilerModuleReflectionData myReflectionData;

    internal CPUProfilerModuleAdapter(object module, CPUProfilerModuleReflectionData reflectionData,
      AbstractAdaptersFactory adaptersFactory)
    {
      myModule = module;
      myReflectionData = reflectionData;
      myAdaptersFactory = adaptersFactory;
    }

    public IProfilerFrameDataHierarchyViewAdapter? GetFrameDataHierarchyView()
    {
      var treeView = myReflectionData.FrameDataHierarchyView?.GetValue(myModule);

      return myAdaptersFactory.CreateProfilerFrameDataHierarchyViewAdapter(treeView);
    }

    private object? GetTimeLineGUIObject()
    {
      if (myReflectionData.TimeLineGUIFieldInfo == null)
      {
        ourLogger.Verbose(
          $"Can't get {nameof(GetTimeLineGUIObject)}: {nameof(myReflectionData.TimeLineGUIFieldInfo)} is null.");
        return null;
      }

      var timeLineGUIObject = myReflectionData.TimeLineGUIFieldInfo.GetValue(myModule);
      return timeLineGUIObject;
    }
  }
}