#nullable enable
using System;
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
    private readonly IProfilerAdaptersFactory myFactory;
    private readonly ProfilerWindow? myProfilerWindow;

    public UnityApiProfilerWindowAdapter(EditorWindow? lastKnownProfilerWindow, IProfilerAdaptersFactory factory)
    {
      myFactory = factory;
      myProfilerWindow = lastKnownProfilerWindow as ProfilerWindow;
    }

    public int GetSelectedFrameIndex()
    {
      return (int)(myProfilerWindow?.selectedFrameIndex ?? -1);
    }

    public void SetSelectedFrameIndex(int frameIndex)
    {
      if (myProfilerWindow == null)
        return;
      
      myProfilerWindow.selectedFrameIndex = frameIndex;
    }

    public object? ProfilerWindowObject => myProfilerWindow;

    public ICPUProfilerModuleAdapter? GetCpuProfilerModule()
    {
      var cpuProfilerModule = myProfilerWindow?.GetFrameTimeViewSampleSelectionController("CPU Usage");
      if (cpuProfilerModule == null)
        return null;
      
      return myFactory.CreateCPUProfilerModuleAdapter(cpuProfilerModule);
    }

    public void SetSelectedThread(int threadId)
    {
      if (myProfilerWindow == null)
        return;

      var cpuModule = myProfilerWindow.GetFrameTimeViewSampleSelectionController(ProfilerWindow.cpuModuleIdentifier);
      if (cpuModule == null)
        throw new InvalidOperationException("CPU Usage Module not found in Profiler Window.");

      cpuModule.focusedThreadIndex = threadId;
    }
    
    public int GetSelectedThreadId()
    {
      if (myProfilerWindow == null)
        return -1 ;

      var cpuModule = myProfilerWindow.GetFrameTimeViewSampleSelectionController(ProfilerWindow.cpuModuleIdentifier);
      if (cpuModule == null)
        throw new InvalidOperationException("CPU Usage Module not found in Profiler Window.");

      return cpuModule.focusedThreadIndex;
    }
  }
}