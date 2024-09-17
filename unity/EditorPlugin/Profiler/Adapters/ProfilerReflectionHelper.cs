using System;
using System.Reflection;
using JetBrains.Diagnostics;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters
{
  internal interface IReflectionData
  {
    bool IsValid();
  }

  internal static class ReflectionDataProvider
  {
    private static readonly ILog ourLogger;
    public static readonly bool IsCompatibleWithCurrentUnityVersion;
    internal static readonly ProfilerDriverReflectionData OurProfilerDriverReflectionData;
    internal static readonly ProfilerWindowReflectionData OurProfilerWindowReflectionData;
    internal static readonly CPUProfilerModuleReflectionData OurCPUProfilerModuleReflectionData;

    internal static readonly ProfilerFrameDataHierarchyViewReflectionData
      OurProfilerFrameDataHierarchyViewReflectionData;

    internal static readonly ProfilerFrameDataTreeViewReflectionData OurProfilerFrameDataTreeViewReflectionData;
    internal static readonly TreeViewControllerReflectionData OurTreeViewControllerReflectionData;

    static ReflectionDataProvider()
    {
      ourLogger = Log.GetLog(nameof(ReflectionDataProvider));
      try
      {
        OurProfilerDriverReflectionData = new ProfilerDriverReflectionData();
        OurProfilerWindowReflectionData = new ProfilerWindowReflectionData();
        OurCPUProfilerModuleReflectionData = new CPUProfilerModuleReflectionData();
        OurProfilerFrameDataHierarchyViewReflectionData = new ProfilerFrameDataHierarchyViewReflectionData();
        OurProfilerFrameDataTreeViewReflectionData = new ProfilerFrameDataTreeViewReflectionData();
        OurTreeViewControllerReflectionData = new TreeViewControllerReflectionData();

        if (!OurProfilerDriverReflectionData.IsValid())
        {
          IsCompatibleWithCurrentUnityVersion = false;
          ourLogger.Verbose(
            $"The {nameof(ProfilerDriverReflectionData)} is not compatible with the current Unity version.");
          return;
        }

        if (!OurProfilerWindowReflectionData.IsValid())
        {
          IsCompatibleWithCurrentUnityVersion = false;
          ourLogger.Verbose(
            $"The {nameof(ProfilerWindowReflectionData)} is not compatible with the current Unity version.");
          return;
        }

        if (!OurCPUProfilerModuleReflectionData.IsValid())
        {
          IsCompatibleWithCurrentUnityVersion = false;
          ourLogger.Verbose(
            $"The {nameof(CPUProfilerModuleReflectionData)} is not compatible with the current Unity version.");
          return;
        }

        if (!OurProfilerFrameDataHierarchyViewReflectionData.IsValid())
        {
          IsCompatibleWithCurrentUnityVersion = false;
          ourLogger.Verbose(
            $"The {nameof(ProfilerFrameDataHierarchyViewReflectionData)} is not compatible with the current Unity version.");
          return;
        }

        if (!OurProfilerFrameDataTreeViewReflectionData.IsValid())
        {
          IsCompatibleWithCurrentUnityVersion = false;
          ourLogger.Verbose(
            $"The {nameof(ProfilerFrameDataTreeViewReflectionData)} is not compatible with the current Unity version.");
          return;
        }

        if (!OurTreeViewControllerReflectionData.IsValid())
        {
          IsCompatibleWithCurrentUnityVersion = false;
          ourLogger.Verbose(
            $"The {nameof(TreeViewControllerReflectionData)} is not compatible with the current Unity version.");
          return;
        }

        IsCompatibleWithCurrentUnityVersion = true;
      }
      catch (Exception e)
      {
        IsCompatibleWithCurrentUnityVersion = false;
        ourLogger.Verbose($"Exception while initializing {nameof(ReflectionDataProvider)}:  {e}");
      }
    }
  }

  internal sealed class ProfilerDriverReflectionData : IReflectionData
  {
    public const string ProfilerDriverTypeName = "UnityEditorInternal.ProfilerDriver";
    public readonly Type ProfilerDriverType;
    public readonly PropertyInfo SelectedPropertyPathPropertyInfo;

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

  internal sealed class ProfilerWindowReflectionData : IReflectionData
  {
    public const string UnityEditorProfilerWindowType = "UnityEditor.ProfilerWindow";
    public readonly FieldInfo ProfilerModulesFieldInfo;
    public readonly Type ProfilerWindowType;

    public ProfilerWindowReflectionData()
    {
      ProfilerWindowType = Assembly.GetAssembly(typeof(EditorWindow)).GetType(UnityEditorProfilerWindowType);
      ProfilerModulesFieldInfo =
        ProfilerWindowType?.GetField("m_ProfilerModules", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? ProfilerWindowType?.GetField("m_AllModules", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    public bool IsValid()
    {
      return ProfilerWindowType != null
             && ProfilerModulesFieldInfo != null
        ;
    }
  }

  internal sealed class CPUProfilerModuleReflectionData : IReflectionData
  {
    public const string CpuProfilerModuleTypeName = "UnityEditorInternal.Profiling.CPUProfilerModule";
    public readonly Type CPUProfilerModuleType;
    public readonly FieldInfo FrameDataHierarchyView;
    public readonly FieldInfo TimeLineGUIFieldInfo;


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

    public readonly MethodInfo InitIfNeededMethodInfo;

    public readonly Type ProfilerFrameDataHierarchyViewType;
    public readonly FieldInfo TreeViewFieldInfo;

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
    public readonly Type ProfilerFrameDataTreeViewType;
    public readonly FieldInfo TreeViewControllerFieldInfo;

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
    public const string TreeViewControllerTypeName = "UnityEditor.IMGUI.Controls.TreeViewController";
    public readonly PropertyInfo ContextClickItemCallback;
    public readonly PropertyInfo ItemDoubleClickedCallbackPropertyInfo;
    public readonly Type TreeViewControllerType;

    public TreeViewControllerReflectionData()
    {
      TreeViewControllerType =
        typeof(EditorWindow).Assembly.GetType(TreeViewControllerTypeName);
      ItemDoubleClickedCallbackPropertyInfo = TreeViewControllerType?
        .GetProperty("itemDoubleClickedCallback",
          BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      ContextClickItemCallback = TreeViewControllerType?
        .GetProperty("contextClickItemCallback",
          BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }

    public bool IsValid()
    {
      return TreeViewControllerType != null
             && ItemDoubleClickedCallbackPropertyInfo != null
             && ItemDoubleClickedCallbackPropertyInfo.PropertyType == typeof(Action<int>)
             && ContextClickItemCallback != null
             && ContextClickItemCallback.PropertyType == typeof(Action<int>)
        ;
    }
  }
}