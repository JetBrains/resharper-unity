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
    public readonly FieldInfo? ProfilerModulesFieldInfo;
    public readonly Type? ProfilerWindowType;
    public readonly MethodInfo? SelectedFrameIndexMethodInfo;

    public ProfilerWindowReflectionData()
    {
      ProfilerWindowType = Assembly.GetAssembly(typeof(EditorWindow)).GetType(UnityEditorProfilerWindowType);
      ProfilerModulesFieldInfo =
        ProfilerWindowType?.GetField("m_ProfilerModules", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? ProfilerWindowType?.GetField("m_AllModules", BindingFlags.Instance | BindingFlags.NonPublic);
      SelectedFrameIndexMethodInfo = ProfilerWindowType?.GetMethod(SelectedFrameIndexPropertyName,
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    }

    public bool IsValid()
    {
      return ProfilerWindowType != null
             && ProfilerModulesFieldInfo != null
             && SelectedFrameIndexMethodInfo != null
        ;
    }
  }
}