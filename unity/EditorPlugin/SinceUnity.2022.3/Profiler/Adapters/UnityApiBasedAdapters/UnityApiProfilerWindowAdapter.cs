#nullable enable
using System;
using JetBrains.Diagnostics;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters.SnapshotNavigation;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEditorInternal.Profiling;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.UnityApiBasedAdapters
{
  public class UnityApiProfilerWindowAdapter : IProfilerWindowAdapter
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(IProfilerWindowAdapter));
    private readonly IProfilerAdaptersFactory myFactory;
    private readonly ProfilerWindow? myProfilerWindow;

    public UnityApiProfilerWindowAdapter(EditorWindow? lastKnownProfilerWindow, IProfilerAdaptersFactory factory)
    {
      myFactory = factory;
      myProfilerWindow = lastKnownProfilerWindow as ProfilerWindow;
    }

    public int GetSelectedFrameIndex()
    {
      if (myProfilerWindow == null)
        return -1;

      try
      {
        return (int)myProfilerWindow.selectedFrameIndex;
      }
      catch (Exception e)
      {
        ourLogger.Verbose($"Failed to get {nameof(GetSelectedFrameIndex)}: {e}");
        return -1;
      }
    }

    public void SetSelectedFrameIndex(int frameIndex)
    {
      if (myProfilerWindow == null)
        return;

      try
      {
        myProfilerWindow.selectedFrameIndex = frameIndex;
      }
      catch (Exception e)
      {
        ourLogger.Verbose($"Failed to set {nameof(SetSelectedFrameIndex)}: {e}");
      }
    }

    public object? ProfilerWindowObject => myProfilerWindow;

    public ICPUProfilerModuleAdapter? GetCpuProfilerModule()
    {
      try
      {
        var cpuProfilerModule = myProfilerWindow?.GetFrameTimeViewSampleSelectionController("CPU Usage");
        if (cpuProfilerModule == null)
          return null;

        return myFactory.CreateCPUProfilerModuleAdapter(cpuProfilerModule);
      }
      catch (Exception e)
      {
        ourLogger.Verbose($"Failed to get {nameof(GetCpuProfilerModule)}: {e}");
        return null;
      }
    }

    public void SetSelectedThread(int threadId)
    {
      if (myProfilerWindow == null)
        return;

      try
      {
        var cpuModule = myProfilerWindow.GetFrameTimeViewSampleSelectionController(ProfilerWindow.cpuModuleIdentifier);
        if (cpuModule == null)
        {
          ourLogger.Verbose($"Can't set {nameof(SetSelectedThread)}: CPU Usage Module not found in Profiler Window.");
          return;
        }

        cpuModule.focusedThreadIndex = threadId;
      }
      catch (Exception e)
      {
        ourLogger.Verbose($"Failed to set {nameof(SetSelectedThread)}: {e}");
      }
    }

    public int GetSelectedThreadId()
    {
      if (myProfilerWindow == null)
        return -1;

      try
      {
        var cpuModule = myProfilerWindow.GetFrameTimeViewSampleSelectionController(ProfilerWindow.cpuModuleIdentifier);
        if (cpuModule == null)
        {
          ourLogger.Verbose($"Can't get {nameof(GetSelectedThreadId)}: CPU Usage Module not found in Profiler Window.");
          return -1;
        }

        return cpuModule.focusedThreadIndex;
      }
      catch (Exception e)
      {
        ourLogger.Verbose($"Failed to get {nameof(GetSelectedThreadId)}: {e}");
        return -1;
      }
    }
  }
}
