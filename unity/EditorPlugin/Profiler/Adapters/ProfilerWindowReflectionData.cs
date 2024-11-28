using System;
using System.Reflection;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.SnapshotNavigation
{
  internal sealed class ProfilerWindowReflectionData : IReflectionData
  {
    public const string UnityEditorProfilerWindowType = "UnityEditor.ProfilerWindow";
    private const string SelectedFrameIndexPropertyName = "GetActiveVisibleFrameIndex";
    public readonly FieldInfo ProfilerModulesFieldInfo;
    public readonly MethodInfo SelectedFrameIndexMethodInfo;
    public readonly Type ProfilerWindowType;

    public ProfilerWindowReflectionData()
    {
      ProfilerWindowType = Assembly.GetAssembly(typeof(EditorWindow)).GetType(UnityEditorProfilerWindowType);
      ProfilerModulesFieldInfo =
        ProfilerWindowType?.GetField("m_ProfilerModules", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? ProfilerWindowType?.GetField("m_AllModules", BindingFlags.Instance | BindingFlags.NonPublic);
      SelectedFrameIndexMethodInfo = ProfilerWindowType?.GetMethod(SelectedFrameIndexPropertyName,
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | BindingFlags.Public);
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