using System;
using System.Collections;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.SnapshotNavigation;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters
{
  internal class ProfilerWindowAdapter
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(ProfilerWindowAdapter));

    public object ProfilerWindowObject { get; }
    private readonly ProfilerWindowReflectionData myReflectionData;

    private ProfilerWindowAdapter(object profilerWindowObject, ProfilerWindowReflectionData reflectionData)
    {
      ProfilerWindowObject = profilerWindowObject;
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

    public int GetSelectedFrameIndex()
    {
      try
      {
        var currentFrameIndex = (int)myReflectionData.SelectedFrameIndexMethodInfo.Invoke(ProfilerWindowObject, null);
        return currentFrameIndex;
      }
      catch (Exception e)
      {
        ourLogger.Verbose($"Invoke of {myReflectionData.SelectedFrameIndexMethodInfo.Name} has failed. {e}");
        return -1;
      }
    }

    [CanBeNull]
    public CPUProfilerModuleAdapter GetCpuProfilerModule(CPUProfilerModuleReflectionData cpuProfilerModuleReflectionData)
    {
      if (!cpuProfilerModuleReflectionData.IsValid())
      {
        ourLogger.Verbose($"{cpuProfilerModuleReflectionData.GetType().Name} is not valid.");
        return null;
      }

      var arrayOfModules = myReflectionData.ProfilerModulesFieldInfo.GetValue(ProfilerWindowObject) as IEnumerable;
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