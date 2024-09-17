using System;
using JetBrains.Diagnostics;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.Profiler
{
  internal class ProfilerWindowFacade : IProfilerWindowSelectionDataProvider
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(ProfilerWindowFacade));
    public static readonly IProfilerWindowSelectionDataProvider Instance = new ProfilerWindowFacade();
    
    private ProfilerWindowAdapter myProfilerWindowAdapter;
    private CPUProfilerModuleAdapter myCPUProfilerModuleAdapter;
    private ProfilerFrameDataHierarchyViewAdapter myProfilerFrameDataHierarchyViewAdapter;
    private ProfilerFrameDataTreeViewAdapter myProfilerFrameDataTreeViewAdapter;
    private TreeViewControllerAdapter myTreeViewControllerAdapter;
    private OnTimeSampleSelected myOnTimeSampleSelected;

    private ProfilerWindowFacade()
    {
      IsSupportingCurrentUnityVersion = ReflectionDataProvider.IsCompatibleWithCurrentUnityVersion;
    }

    public bool IsInitialized { get; private set; }
    public bool IsSupportingCurrentUnityVersion { get; }

    public void Deinit(EditorWindow profilerWindow, OnTimeSampleSelected onTimeSampleSelected)
    {
      if (!IsSupportingCurrentUnityVersion)
        return;

      if (!IsInitialized)
        return;

      try
      {
        myTreeViewControllerAdapter.ItemDoubleClicked -= MyTreeViewControllerAdapterOnItemDoubleClicked;
      }
      catch (Exception e)
      {
        ourLogger.Verbose($"Failed to init ProfilerWindowFacade: {e}");
      }
      finally
      {
        myOnTimeSampleSelected -= onTimeSampleSelected;
        IsInitialized = false;
        myProfilerWindowAdapter = null;
        myCPUProfilerModuleAdapter = null;
        myProfilerFrameDataHierarchyViewAdapter = null;
        myProfilerFrameDataTreeViewAdapter = null;
        myTreeViewControllerAdapter = null;
      }
    }

    public void Init(EditorWindow profilerWindow, OnTimeSampleSelected onTimeSampleSelected)
    {
      try
      {
        IsInitialized = InternalInit(profilerWindow);
        myOnTimeSampleSelected += onTimeSampleSelected;
        ourLogger.Verbose($"{nameof(ProfilerWindowFacade)} has been initialized.");
      }
      catch (Exception e)
      {
        ourLogger.Verbose($"Failed to init {nameof(ProfilerWindowFacade)}: {e}");
        IsInitialized = false;
      }
    }


    private bool InternalInit(EditorWindow profilerWindow)
    {
      myProfilerWindowAdapter =
        ProfilerWindowAdapter.Create(profilerWindow, ReflectionDataProvider.OurProfilerWindowReflectionData);
      if (myProfilerWindowAdapter == null)
      {
        ourLogger.Verbose($"Failed to create {nameof(ProfilerWindowAdapter)} for {nameof(ProfilerWindowFacade)}");
        return false;
      }

      myCPUProfilerModuleAdapter = myProfilerWindowAdapter.GetCpuProfilerModule();
      if (myCPUProfilerModuleAdapter == null)
      {
        ourLogger.Verbose($"Failed to create {nameof(CPUProfilerModuleAdapter)} for {nameof(ProfilerWindowFacade)}");
        return false;
      }

      myProfilerFrameDataHierarchyViewAdapter = myCPUProfilerModuleAdapter.GetFrameDataHierarchyView();
      if (myProfilerFrameDataHierarchyViewAdapter == null)
      {
        ourLogger.Verbose(
          $"Failed to create {nameof(ProfilerFrameDataHierarchyViewAdapter)} for {nameof(ProfilerWindowFacade)}");
        return false;
      }

      myProfilerFrameDataHierarchyViewAdapter.InitIfNeeded();

      myProfilerFrameDataTreeViewAdapter = myProfilerFrameDataHierarchyViewAdapter.GetTreeView();
      if (myProfilerFrameDataTreeViewAdapter == null)
      {
        ourLogger.Verbose(
          $"Failed to create {nameof(ProfilerFrameDataTreeViewAdapter)} for {nameof(ProfilerWindowFacade)}");
        return false;
      }

      myTreeViewControllerAdapter = myProfilerFrameDataTreeViewAdapter.GetTreeViewController();
      if (myTreeViewControllerAdapter == null)
      {
        ourLogger.Verbose($"Failed to create {nameof(TreeViewControllerAdapter)} for {nameof(ProfilerWindowFacade)}");
        return false;
      }

      myTreeViewControllerAdapter.ItemDoubleClicked += MyTreeViewControllerAdapterOnItemDoubleClicked;
      return true;
    }

    private void MyTreeViewControllerAdapterOnItemDoubleClicked(int obj)
    {
      try
      {
        var selectedPropertyPath = ProfilerDriverAdapter.GetSelectedPropertyPath();
        myOnTimeSampleSelected?.Invoke(selectedPropertyPath, selectedPropertyPath);
      }
      catch (Exception e)
      {
        ourLogger.Verbose($"Failed to handle double click on {nameof(ProfilerWindowFacade)}: {e}");
      }
    }
  }
}