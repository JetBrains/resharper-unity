using System.Collections;
using JetBrains.Annotations;
using JetBrains.Diagnostics;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters
{
  internal class ProfilerWindowAdapter
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(ProfilerWindowAdapter));

    private readonly object myProfilerWindowObject;
    private readonly ProfilerWindowReflectionData myReflectionData;

    private ProfilerWindowAdapter(object profilerWindowObject, ProfilerWindowReflectionData reflectionData)
    {
      myProfilerWindowObject = profilerWindowObject;
      myReflectionData = reflectionData;
    }

    public static ProfilerWindowAdapter Create(object profilerWindowObject,
      ProfilerWindowReflectionData reflectionData)
    {
      if (!reflectionData.IsValid())
      {
        ourLogger.Verbose($"{reflectionData.GetType().Name} is not valid.");
        return null;
      }

      if (profilerWindowObject?.GetType() != reflectionData.ProfilerWindowType)
      {
        ourLogger.Verbose($"Type '{ProfilerWindowReflectionData.UnityEditorProfilerWindowType}' is expected.");
        return null;
      }

      return new ProfilerWindowAdapter(profilerWindowObject, reflectionData);
    }

    [CanBeNull]
    public CPUProfilerModuleAdapter GetCpuProfilerModule(CPUProfilerModuleReflectionData cpuProfilerModuleReflectionData)
    {
      if (!cpuProfilerModuleReflectionData.IsValid())
      {
        ourLogger.Verbose($"{cpuProfilerModuleReflectionData.GetType().Name} is not valid.");
        return null;
      }

      var arrayOfModules = myReflectionData.ProfilerModulesFieldInfo.GetValue(myProfilerWindowObject) as IEnumerable;
      if (arrayOfModules == null)
      {
        ourLogger.Verbose($"{myReflectionData.ProfilerModulesFieldInfo.Name} returned null.");
        return null;
      }

      foreach (var module in arrayOfModules)
        if (module.GetType() == cpuProfilerModuleReflectionData.CPUProfilerModuleType)
          return CPUProfilerModuleAdapter.Create(module, cpuProfilerModuleReflectionData);

      return null;
    }
  }
}