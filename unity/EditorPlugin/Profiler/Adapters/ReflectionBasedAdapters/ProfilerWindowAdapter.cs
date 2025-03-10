#nullable enable
using System;
using System.Collections;
using JetBrains.Diagnostics;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters.SnapshotNavigation;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters
{
  internal class ProfilerWindowAdapter : IProfilerWindowAdapter
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(IProfilerWindowAdapter));
    private readonly CPUProfilerModuleReflectionData? myCPUProfilerModuleReflectionData;
    private readonly ReflectionBasedAdaptersFactory myReflectionBasedAdaptersFactory;
    private readonly ProfilerWindowReflectionData? myReflectionData;

    internal ProfilerWindowAdapter(object? profilerWindowObject, ProfilerWindowReflectionData? reflectionData,
      ReflectionBasedAdaptersFactory reflectionBasedAdaptersFactory,
      CPUProfilerModuleReflectionData? cpuProfilerModuleReflectionData)
    {
      ProfilerWindowObject = profilerWindowObject;
      myReflectionData = reflectionData;
      myReflectionBasedAdaptersFactory = reflectionBasedAdaptersFactory;
      myCPUProfilerModuleReflectionData = cpuProfilerModuleReflectionData;
    }

    public object? ProfilerWindowObject { get; }

    public int GetSelectedFrameIndex()
    {
      if (myReflectionData == null)
      {
        ourLogger.Verbose($"Can't get {nameof(GetSelectedFrameIndex)}: {nameof(myReflectionData)} is null.");
        return -1;
      }

      if (myReflectionData.SelectedFrameIndexMethodInfo == null)
      {
        ourLogger.Verbose(
          $"Can't get {nameof(GetSelectedFrameIndex)}: {nameof(myReflectionData.SelectedFrameIndexMethodInfo)} is null.");
        return -1;
      }

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

    public ICPUProfilerModuleAdapter? GetCpuProfilerModule()
    {
      if (myCPUProfilerModuleReflectionData == null)
      {
        ourLogger.Verbose(
          $"Can't get {nameof(GetCpuProfilerModule)}: {nameof(myCPUProfilerModuleReflectionData)} is null.");
        return null;
      }

      if (!myCPUProfilerModuleReflectionData.IsValid())
      {
        ourLogger.Verbose($"{myCPUProfilerModuleReflectionData.GetType().Name} is not valid.");
        return null;
      }

      if (myReflectionData == null)
      {
        ourLogger.Verbose($"Can't get {nameof(GetCpuProfilerModule)}: {nameof(myReflectionData)} is null.");
        return null;
      }

      if (myReflectionData.ProfilerModulesFieldInfo == null)
      {
        ourLogger.Verbose(
          $"Can't get {nameof(GetCpuProfilerModule)}: {nameof(myReflectionData.ProfilerModulesFieldInfo)} is null.");
        return null;
      }

      var arrayOfModules = myReflectionData.ProfilerModulesFieldInfo.GetValue(ProfilerWindowObject) as IEnumerable;
      if (arrayOfModules == null)
      {
        ourLogger.Verbose($"{myReflectionData.ProfilerModulesFieldInfo.Name} returned null.");
        return null;
      }

      foreach (var module in arrayOfModules)
        if (module.GetType() == myCPUProfilerModuleReflectionData.CPUProfilerModuleType)
          return myReflectionBasedAdaptersFactory.CreateCPUProfilerModuleAdapter(module);

      return null;
    }
  }
}