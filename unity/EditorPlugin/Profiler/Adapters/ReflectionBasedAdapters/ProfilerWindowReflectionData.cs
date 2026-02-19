#nullable enable
using System;
using System.Reflection;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters
{
  internal sealed class ProfilerWindowReflectionData : IReflectionData
  {
    public const string UnityEditorProfilerWindowType = "UnityEditor.ProfilerWindow";
    private const string SelectedFrameIndexPropertyName = "GetActiveVisibleFrameIndex";
    private const string SetSelectedFrameIndexPropertyName = "SetActiveVisibleFrameIndex";
    private const string GetCPUModuleMethodName = "GetFrameTimeViewSampleSelectionController";
    public readonly FieldInfo? ProfilerModulesFieldInfo;
    public readonly Type? ProfilerWindowType;
    public readonly MethodInfo? SelectedFrameIndexMethodInfo;
    public readonly MethodInfo? SetSelectedFrameIndexMethodInfo;
    public readonly MethodInfo? GetCPUModuleMethodInfo;

    public ProfilerWindowReflectionData()
    {
      ProfilerWindowType = Assembly.GetAssembly(typeof(EditorWindow)).GetType(UnityEditorProfilerWindowType);
      ProfilerModulesFieldInfo =
        ProfilerWindowType?.GetField("m_ProfilerModules", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? ProfilerWindowType?.GetField("m_AllModules", BindingFlags.Instance | BindingFlags.NonPublic);
      SelectedFrameIndexMethodInfo = ProfilerWindowType?.GetMethod(SelectedFrameIndexPropertyName,
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
      SetSelectedFrameIndexMethodInfo = ProfilerWindowType?.GetMethod(SetSelectedFrameIndexPropertyName,
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
      GetCPUModuleMethodInfo = ProfilerWindowType?.GetMethod(GetCPUModuleMethodName,
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    }

    public bool IsValid()
    {
      return ProfilerWindowType != null
             && ProfilerModulesFieldInfo != null
             && SelectedFrameIndexMethodInfo != null
             && SetSelectedFrameIndexMethodInfo != null
             && GetCPUModuleMethodInfo != null
        ;
    }
  }
}