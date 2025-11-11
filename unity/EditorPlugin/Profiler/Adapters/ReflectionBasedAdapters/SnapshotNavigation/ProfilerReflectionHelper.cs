#nullable enable
using System;
using System.Reflection;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters.SnapshotNavigation
{
  internal sealed class ProfilerDriverReflectionData : IReflectionData
  {
    private const string ProfilerDriverTypeName = "UnityEditorInternal.ProfilerDriver";
    private readonly Type? ProfilerDriverType;
    public readonly PropertyInfo? SelectedPropertyPathPropertyInfo;

    public ProfilerDriverReflectionData()
    {
      ProfilerDriverType = typeof(EditorWindow).Assembly.GetType(ProfilerDriverTypeName);
      SelectedPropertyPathPropertyInfo = ProfilerDriverType?.GetProperty("selectedPropertyPath",
        BindingFlags.Static | BindingFlags.Public);
    }

    public bool IsValid()
    {
      return ProfilerDriverType != null
             && SelectedPropertyPathPropertyInfo != null;
    }
  }

  internal sealed class CPUProfilerModuleReflectionData : IReflectionData
  {
    public const string CpuProfilerModuleTypeName = "UnityEditorInternal.Profiling.CPUProfilerModule";
    public readonly Type? CPUProfilerModuleType;
    public readonly FieldInfo? FrameDataHierarchyView;
    public readonly FieldInfo? TimeLineGUIFieldInfo;


    public CPUProfilerModuleReflectionData()
    {
      CPUProfilerModuleType =
        typeof(EditorWindow).Assembly.GetType(CpuProfilerModuleTypeName);
      TimeLineGUIFieldInfo =
        CPUProfilerModuleType?.GetField("m_TimelineGUI", BindingFlags.Instance | BindingFlags.NonPublic);
      FrameDataHierarchyView = CPUProfilerModuleType?.GetField("m_FrameDataHierarchyView",
        BindingFlags.Instance | BindingFlags.NonPublic);
    }

    public bool IsValid()
    {
      return CPUProfilerModuleType != null && FrameDataHierarchyView != null && TimeLineGUIFieldInfo != null;
    }
  }

  internal sealed class ProfilerFrameDataHierarchyViewReflectionData : IReflectionData
  {
    public const string FrameDataHierarchyViewTypeName =
      "UnityEditorInternal.Profiling.ProfilerFrameDataHierarchyView";

    public readonly MethodInfo? InitIfNeededMethodInfo;

    public readonly Type? ProfilerFrameDataHierarchyViewType;
    public readonly FieldInfo? TreeViewFieldInfo;

    public ProfilerFrameDataHierarchyViewReflectionData()
    {
      ProfilerFrameDataHierarchyViewType =
        typeof(EditorWindow).Assembly.GetType(FrameDataHierarchyViewTypeName);
      TreeViewFieldInfo =
        ProfilerFrameDataHierarchyViewType?.GetField("m_TreeView",
          BindingFlags.Instance | BindingFlags.NonPublic);

      InitIfNeededMethodInfo = ProfilerFrameDataHierarchyViewType?.GetMethod("InitIfNeeded",
        BindingFlags.Instance | BindingFlags.NonPublic);
    }

    public bool IsValid()
    {
      return ProfilerFrameDataHierarchyViewType != null
             && TreeViewFieldInfo != null
             && InitIfNeededMethodInfo != null
        ;
    }
  }

  internal sealed class ProfilerFrameDataTreeViewReflectionData : IReflectionData
  {
    public const string ProfilerFrameDataTreeViewTypeName = "UnityEditorInternal.ProfilerFrameDataTreeView";
    public readonly Type? ProfilerFrameDataTreeViewType;
    public readonly FieldInfo? TreeViewControllerFieldInfo;

    public ProfilerFrameDataTreeViewReflectionData()
    {
      ProfilerFrameDataTreeViewType =
        typeof(EditorWindow).Assembly.GetType(ProfilerFrameDataTreeViewTypeName);
      TreeViewControllerFieldInfo =
        ProfilerFrameDataTreeViewType?.BaseType?.GetField("m_TreeView",
          BindingFlags.Instance | BindingFlags.NonPublic);
    }

    public bool IsValid()
    {
      return ProfilerFrameDataTreeViewType != null && TreeViewControllerFieldInfo != null;
    }
  }

  internal sealed class TreeViewControllerReflectionData : IReflectionData
  {
    public readonly PropertyInfo? ContextClickItemCallback;
    public readonly PropertyInfo? ItemDoubleClickedCallbackPropertyInfo;

    public TreeViewControllerReflectionData(Type type)
    {
      ContextClickItemCallback = GetClickItemProperty(type);
      ItemDoubleClickedCallbackPropertyInfo = GetDoubleClickProperty(type);
    }

    private static PropertyInfo? GetClickItemProperty(Type treeViewControllerType)
    {
      return treeViewControllerType?
        .GetProperty("contextClickItemCallback",
          BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }

    private static PropertyInfo? GetDoubleClickProperty(Type treeViewControllerType)
    {
      return treeViewControllerType?
        .GetProperty("itemDoubleClickedCallback",
          BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }

    public bool IsValid()
    {
      return ItemDoubleClickedCallbackPropertyInfo != null
      && ItemDoubleClickedCallbackPropertyInfo.PropertyType == typeof(Action<int>)
      && ContextClickItemCallback != null
      && ContextClickItemCallback.PropertyType == typeof(Action<int>)
      ;
    }
  }
}