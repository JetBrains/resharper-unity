using JetBrains.Annotations;
using JetBrains.Diagnostics;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters
{
  internal class CPUProfilerModuleAdapter
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(CPUProfilerModuleAdapter));

    private readonly object myModule;
    private readonly CPUProfilerModuleReflectionData myReflectionData;

    private CPUProfilerModuleAdapter(object module, CPUProfilerModuleReflectionData reflectionData)
    {
      myModule = module;
      myReflectionData = reflectionData;
    }

    public static CPUProfilerModuleAdapter Create(object module, CPUProfilerModuleReflectionData reflectionData)
    {
      if (!reflectionData.IsValid())
      {
        ourLogger.Verbose($"{reflectionData.GetType().Name} is not valid.");
        return null;
      }

      if (module.GetType() != reflectionData.CPUProfilerModuleType)
      {
        ourLogger.Verbose($"Type '{CPUProfilerModuleReflectionData.CpuProfilerModuleTypeName}' expected.");
        return null;
      }

      return new CPUProfilerModuleAdapter(module, reflectionData);
    }

    [CanBeNull]
    public ProfilerFrameDataHierarchyViewAdapter GetFrameDataHierarchyView()
    {
      return ProfilerFrameDataHierarchyViewAdapter.Create(
        myReflectionData.FrameDataHierarchyView.GetValue(myModule),
        ReflectionDataProvider.OurProfilerFrameDataHierarchyViewReflectionData);
    }

    [CanBeNull]
    public TimeLineGUIAdapter GetTimeLineGUIFieldInfo()
    {
      return TimeLineGUIAdapter.Create(myReflectionData.TimeLineGUIFieldInfo.GetValue(myModule));
    }
  }
}