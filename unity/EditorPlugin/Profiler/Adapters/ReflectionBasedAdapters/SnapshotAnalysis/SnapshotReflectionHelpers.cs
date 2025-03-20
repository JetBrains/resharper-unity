#nullable enable
using System;
using System.Reflection;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters.SnapshotAnalysis
{
  internal sealed class ProfilerSnapshotDriverReflectionData : IReflectionData
  {
    private const string ProfilerDriverTypeName = "UnityEditorInternal.ProfilerDriver";
    private const string GetRawFrameDataViewMethodName = "GetRawFrameDataView";
    public readonly MethodInfo GetRawFrameDataViewMethod;
    private readonly Type ProfilerDriverType;

    public ProfilerSnapshotDriverReflectionData()
    {
      ProfilerDriverType = Assembly.GetAssembly(typeof(EditorWindow)).GetType(ProfilerDriverTypeName);
      GetRawFrameDataViewMethod =
        ProfilerDriverType.GetMethod(GetRawFrameDataViewMethodName, BindingFlags.Static | BindingFlags.Public);
    }

    public bool IsValid()
    {
      return ProfilerDriverType != null
             && GetRawFrameDataViewMethod != null;
    }
  }

  internal sealed class RawFrameDataViewReflectionData : IReflectionData
  {
    private const string RawFrameDataViewTypeName = "UnityEditor.Profiling.RawFrameDataView";
    private const string GetSampleTimeMsMethodName = "GetSampleTimeMs";
    private const string GetSampleMarkerIdMethodName = "GetSampleMarkerId";
    private const string GetSampleChildrenCountMethodName = "GetSampleChildrenCount";
    private const string GetSampleNameMethodName = "GetSampleName";
    private const string FrameStartTimeMsPropertyName = "frameStartTimeMs";
    private const string FrameTimeMsPropertyName = "frameTimeMs";
    private const string SampleCountPropertyName = "sampleCount";
    private const string ThreadNamePropertyName = "threadName";
    private const string ThreadIndexPropertyName = "threadIndex";
    private const string ValidPropertyName = "valid";

    public readonly PropertyInfo FrameStartTimeMsProperty;
    public readonly PropertyInfo FrameTimeMsProperty;
    public readonly MethodInfo GetSampleChildrenCountMethod;
    public readonly MethodInfo GetSampleMarkerIdMethod;
    public readonly MethodInfo GetSampleNameMethod;
    public readonly MethodInfo GetSampleTimeMsMethod;
    public readonly Type RawFrameDataViewType;
    public readonly PropertyInfo SampleCountProperty;
    public readonly PropertyInfo ThreadIndexProperty;
    public readonly PropertyInfo ThreadNameProperty;
    public readonly PropertyInfo ValidProperty;

    public RawFrameDataViewReflectionData()
    {
      RawFrameDataViewType = Assembly.GetAssembly(typeof(EditorWindow)).GetType(RawFrameDataViewTypeName);
      GetSampleTimeMsMethod =
        RawFrameDataViewType.GetMethod(GetSampleTimeMsMethodName, BindingFlags.Instance | BindingFlags.Public);
      GetSampleMarkerIdMethod =
        RawFrameDataViewType.GetMethod(GetSampleMarkerIdMethodName, BindingFlags.Instance | BindingFlags.Public);

      GetSampleChildrenCountMethod = RawFrameDataViewType.GetMethod(GetSampleChildrenCountMethodName,
        BindingFlags.Instance | BindingFlags.Public);
      GetSampleNameMethod =
        RawFrameDataViewType.GetMethod(GetSampleNameMethodName, BindingFlags.Instance | BindingFlags.Public);

      FrameStartTimeMsProperty =
        RawFrameDataViewType.GetProperty(FrameStartTimeMsPropertyName, BindingFlags.Instance | BindingFlags.Public);
      FrameTimeMsProperty =
        RawFrameDataViewType.GetProperty(FrameTimeMsPropertyName, BindingFlags.Instance | BindingFlags.Public);
      SampleCountProperty =
        RawFrameDataViewType.GetProperty(SampleCountPropertyName, BindingFlags.Instance | BindingFlags.Public);
      ThreadNameProperty =
        RawFrameDataViewType.GetProperty(ThreadNamePropertyName, BindingFlags.Instance | BindingFlags.Public);
      ThreadIndexProperty =
        RawFrameDataViewType.GetProperty(ThreadIndexPropertyName, BindingFlags.Instance | BindingFlags.Public);
      ValidProperty = RawFrameDataViewType.GetProperty(ValidPropertyName, BindingFlags.Instance | BindingFlags.Public);
    }

    public bool IsValid()
    {
      return RawFrameDataViewType != null
             && GetSampleTimeMsMethod != null
             && GetSampleMarkerIdMethod != null
             && GetSampleChildrenCountMethod != null
             && GetSampleNameMethod != null
             && FrameStartTimeMsProperty != null
             && FrameTimeMsProperty != null
             && SampleCountProperty != null
             && ThreadNameProperty != null
             && ThreadIndexProperty != null;
    }
  }
}