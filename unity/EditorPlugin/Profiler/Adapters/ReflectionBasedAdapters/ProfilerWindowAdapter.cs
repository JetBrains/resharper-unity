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

    public void SetSelectedFrameIndex(int frameIndex)
    {
      if (myReflectionData == null)
      {
        ourLogger.Verbose($"Can't set {nameof(SetSelectedFrameIndex)}: {nameof(myReflectionData)} is null.");
        return;
      }

      if (myReflectionData.SetSelectedFrameIndexMethodInfo == null)
      {
        ourLogger.Verbose(
          $"Can't set {nameof(SetSelectedFrameIndex)}: {nameof(myReflectionData.SetSelectedFrameIndexMethodInfo)} is null.");
        return;
      }

      try
      {
        myReflectionData.SetSelectedFrameIndexMethodInfo.Invoke(ProfilerWindowObject, new object[] { frameIndex });
      }
      catch (Exception e)
      {
        ourLogger.Verbose($"Invoke of {myReflectionData.SetSelectedFrameIndexMethodInfo.Name} has failed. {e}");
      }
    }

    public void SetSelectedThread(int threadId)
    {
      if (myReflectionData == null)
      {
        ourLogger.Verbose($"Can't set {nameof(SetSelectedThread)}: {nameof(myReflectionData)} is null.");
        return;
      }

      if (myReflectionData.GetCPUModuleMethodInfo == null)
      {
        ourLogger.Verbose(
          $"Can't set {nameof(SetSelectedThread)}: {nameof(myReflectionData.GetCPUModuleMethodInfo)} is null.");
        return;
      }

      if (myCPUProfilerModuleReflectionData == null)
      {
        ourLogger.Verbose(
          $"Can't set {nameof(SetSelectedThread)}: {nameof(myCPUProfilerModuleReflectionData)} is null.");
        return;
      }

      if (myCPUProfilerModuleReflectionData.FocusedThreadIndexPropertyInfo == null)
      {
        ourLogger.Verbose(
          $"Can't set {nameof(SetSelectedThread)}: {nameof(myCPUProfilerModuleReflectionData.FocusedThreadIndexPropertyInfo)} is null.");
        return;
      }

      try
      {
        var cpuModule = myReflectionData.GetCPUModuleMethodInfo.Invoke(ProfilerWindowObject, new object[] { "CPU Usage" });
        if (cpuModule == null)
        {
          ourLogger.Verbose("CPU Usage Module not found in Profiler Window.");
          return;
        }

        myCPUProfilerModuleReflectionData.FocusedThreadIndexPropertyInfo.SetValue(cpuModule, threadId);
      }
      catch (Exception e)
      {
        ourLogger.Verbose($"Failed to set selected thread: {e}");
      }
    }

    public int GetSelectedThreadId()
    {
      if (myReflectionData == null)
      {
        ourLogger.Verbose($"Can't get {nameof(GetSelectedThreadId)}: {nameof(myReflectionData)} is null.");
        return -1;
      }

      if (myReflectionData.GetCPUModuleMethodInfo == null)
      {
        ourLogger.Verbose(
          $"Can't get {nameof(GetSelectedThreadId)}: {nameof(myReflectionData.GetCPUModuleMethodInfo)} is null.");
        return -1;
      }

      if (myCPUProfilerModuleReflectionData == null)
      {
        ourLogger.Verbose(
          $"Can't get {nameof(GetSelectedThreadId)}: {nameof(myCPUProfilerModuleReflectionData)} is null.");
        return -1;
      }

      if (myCPUProfilerModuleReflectionData.FocusedThreadIndexPropertyInfo == null)
      {
        ourLogger.Verbose(
          $"Can't get {nameof(GetSelectedThreadId)}: {nameof(myCPUProfilerModuleReflectionData.FocusedThreadIndexPropertyInfo)} is null.");
        return -1;
      }

      try
      {
        var cpuModule = myReflectionData.GetCPUModuleMethodInfo.Invoke(ProfilerWindowObject, new object[] { "CPU Usage" });
        if (cpuModule == null)
        {
          ourLogger.Verbose("CPU Usage Module not found in Profiler Window.");
          return -1;
        }

        return (int)myCPUProfilerModuleReflectionData.FocusedThreadIndexPropertyInfo.GetValue(cpuModule);
      }
      catch (Exception e)
      {
        ourLogger.Verbose($"Failed to get selected thread: {e}");
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