#nullable enable
using JetBrains.Diagnostics;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters.SnapshotNavigation;
using JetBrains.Rider.Unity.Editor.Profiler.SnapshotNavigation;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters
{
  internal abstract class AbstractAdaptersFactory : IProfilerAdaptersFactory
  {
    protected static readonly ILog OurLogger = Log.GetLog(nameof(ReflectionBasedAdaptersFactory));
    protected readonly ReflectionDataProvider MyReflectionDataProvider = new();

    public abstract IProfilerWindowSelectionDataProvider? CreateProfilerWindowFacade();
    public abstract IProfilerSnapshotDriverAdapter? CreateProfilerSnapshotDriverAdapter();
    public abstract IProfilerWindowAdapter? CreateProfilerWindowAdapter(EditorWindow? lastKnownProfilerWindow);
    public abstract IProfilerWindowTypeChecker? CreateProfilerWindowTypeChecker();

    public ITreeViewControllerAdapter? TryCreateTreeViewControllerAdapter(EditorWindow profilerWindow)
    {
      var profilerWindowAdapter = CreateProfilerWindowAdapter(profilerWindow);
      if (profilerWindowAdapter == null)
      {
        OurLogger.Verbose($"Failed to create {nameof(ProfilerWindowAdapter)} for {nameof(ProfilerWindowFacade)}");
        return null;
      }

      var cpuProfilerModuleAdapter = CreateCPUProfilerModuleAdapter(profilerWindowAdapter);
      if (cpuProfilerModuleAdapter == null)
      {
        OurLogger.Verbose($"Failed to create {nameof(CPUProfilerModuleAdapter)} for {nameof(ProfilerWindowFacade)}");
        return null;
      }

      var profilerFrameDataHierarchyViewAdapter =
        CreateProfilerFrameDataHierarchyViewAdapter(cpuProfilerModuleAdapter);
      if (profilerFrameDataHierarchyViewAdapter == null)
      {
        OurLogger.Verbose(
          $"Failed to create {nameof(profilerFrameDataHierarchyViewAdapter)} for {nameof(ProfilerWindowFacade)}");
        return null;
      }

      profilerFrameDataHierarchyViewAdapter.InitIfNeeded();

      var profilerFrameDataTreeViewAdapter =
        CreateProfilerFrameDataTreeViewAdapter(profilerFrameDataHierarchyViewAdapter);
      if (profilerFrameDataTreeViewAdapter == null)
      {
        OurLogger.Verbose(
          $"Failed to create {nameof(ProfilerFrameDataTreeViewAdapter)} for {nameof(ProfilerWindowFacade)}");
        return null;
      }

      var treeViewControllerAdapter = CreateTreeViewControllerAdapter(profilerFrameDataTreeViewAdapter);
      if (treeViewControllerAdapter == null)
      {
        OurLogger.Verbose($"Failed to create {nameof(TreeViewControllerAdapter)} for {nameof(ProfilerWindowFacade)}");
        return null;
      }

      return treeViewControllerAdapter;
    }

    public ICPUProfilerModuleAdapter? CreateCPUProfilerModuleAdapter(object module)
    {
      var reflectionData = MyReflectionDataProvider.CPUProfilerModuleReflectionData;

      if (reflectionData == null || !reflectionData.IsValid())
      {
        OurLogger.Verbose($"{reflectionData!.GetType().Name} is not valid.");
        return null;
      }

      if (module.GetType() != reflectionData.CPUProfilerModuleType)
      {
        OurLogger.Verbose($"Type '{CPUProfilerModuleReflectionData.CpuProfilerModuleTypeName}' expected.");
        return null;
      }

      return new CPUProfilerModuleAdapter(module, reflectionData, this);
    }

    private static ICPUProfilerModuleAdapter? CreateCPUProfilerModuleAdapter(
      IProfilerWindowAdapter? profilerWindowAdapter)
    {
      return profilerWindowAdapter?.GetCpuProfilerModule();
    }

    private static IProfilerFrameDataHierarchyViewAdapter? CreateProfilerFrameDataHierarchyViewAdapter(
      ICPUProfilerModuleAdapter cpuProfilerModuleAdapter)
    {
      return cpuProfilerModuleAdapter?.GetFrameDataHierarchyView();
    }

    private static IProfilerFrameDataTreeViewAdapter? CreateProfilerFrameDataTreeViewAdapter(
      IProfilerFrameDataHierarchyViewAdapter profilerFrameDataHierarchyViewAdapter)
    {
      if (profilerFrameDataHierarchyViewAdapter is not ProfilerFrameDataHierarchyViewAdapter hierarchyViewAdapter)
        return null;

      return hierarchyViewAdapter.GetTreeView();
    }

    private static ITreeViewControllerAdapter? CreateTreeViewControllerAdapter(
      IProfilerFrameDataTreeViewAdapter profilerFrameDataTreeViewAdapter)
    {
      return profilerFrameDataTreeViewAdapter is ProfilerFrameDataTreeViewAdapter treeViewAdapter
        ? treeViewAdapter.GetTreeViewController()
        : null;
    }

    internal ProfilerFrameDataHierarchyViewAdapter? CreateProfilerFrameDataHierarchyViewAdapter(object? hierarchyView)
    {
      var reflectionData = MyReflectionDataProvider.ProfilerFrameDataHierarchyViewReflectionData;

      if (reflectionData == null)
      {
        OurLogger.Verbose($"{nameof(MyReflectionDataProvider.ProfilerFrameDataHierarchyViewReflectionData)} is null.");
        return null;
      }

      if (!reflectionData.IsValid())
      {
        OurLogger.Verbose($"{reflectionData.GetType().Name} is not valid.");
        return null;
      }

      if (hierarchyView == null)
      {
        OurLogger.Verbose($"{reflectionData.GetType().Name} object is null");
        return null;
      }

      if (hierarchyView.GetType() != reflectionData.ProfilerFrameDataHierarchyViewType)
      {
        OurLogger.Verbose(
          $"Type '{ProfilerFrameDataHierarchyViewReflectionData.FrameDataHierarchyViewTypeName}' expected.");
        return null;
      }

      return new ProfilerFrameDataHierarchyViewAdapter(hierarchyView, reflectionData, this);
    }

    internal ProfilerFrameDataTreeViewAdapter? CreateProfilerFrameDataTreeViewAdapter(object treeView)
    {
      var reflectionData = MyReflectionDataProvider.ProfilerFrameDataTreeViewReflectionData;

      if (reflectionData == null)
      {
        OurLogger.Verbose($"{nameof(MyReflectionDataProvider.ProfilerFrameDataTreeViewReflectionData)} is null.");
        return null;
      }

      if (!reflectionData.IsValid())
      {
        OurLogger.Verbose($"{reflectionData.GetType().Name} is not valid.");
        return null;
      }

      if (treeView?.GetType() != reflectionData.ProfilerFrameDataTreeViewType)
      {
        OurLogger.Verbose(
          $"Type '{ProfilerFrameDataTreeViewReflectionData.ProfilerFrameDataTreeViewTypeName}' expected.");
        return null;
      }

      return new ProfilerFrameDataTreeViewAdapter(treeView, reflectionData, this);
    }

    internal TreeViewControllerAdapter? CreateTreeViewControllerAdapter(object treeViewController)
    {
      var reflectionData = MyReflectionDataProvider.TreeViewControllerReflectionData;

      if (reflectionData == null || !reflectionData.IsValid())
      {
        OurLogger.Verbose($"{reflectionData?.GetType().Name} is not valid.");
        return null;
      }

      if (treeViewController.GetType() != reflectionData.TreeViewControllerType)
      {
        OurLogger.Verbose($"Type '{TreeViewControllerReflectionData.TreeViewControllerTypeName}' expected.");
        return null;
      }

      return new TreeViewControllerAdapter(treeViewController, reflectionData);
    }
  }
}