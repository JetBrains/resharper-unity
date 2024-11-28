using System;
using System.Reflection;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.SnapshotAnalysis
{
  internal sealed class ProfilerSnapshotDriverReflectionData : IReflectionData
  {
    public const string ProfilerDriverTypeName = "UnityEditorInternal.ProfilerDriver";
    public readonly Type ProfilerDriverType;

    public const string GetRawFrameDataViewMethodName = "GetRawFrameDataView";
    public readonly MethodInfo GetRawFrameDataViewMethod;

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
    public const string RawFrameDataViewTypeName = "UnityEditor.Profiling.RawFrameDataView";
    public readonly Type RawFrameDataViewType;

    public const string GetSampleTimeMsMethodName = "GetSampleTimeMs";
    public readonly MethodInfo GetSampleTimeMsMethod;

    public const string GetSampleMarkerIdMethodName = "GetSampleMarkerId";
    public readonly MethodInfo GetSampleMarkerIdMethod;

    public const string GetSampleChildrenCountMethodName = "GetSampleChildrenCount";
    public readonly MethodInfo GetSampleChildrenCountMethod;

    public const string GetSampleNameMethodName = "GetSampleName";
    public readonly MethodInfo GetSampleNameMethod;

    // New property constants and PropertyInfo
    public const string FrameStartTimeMsPropertyName = "frameStartTimeMs";
    public readonly PropertyInfo FrameStartTimeMsProperty;

    public const string FrameTimeMsPropertyName = "frameTimeMs";
    public readonly PropertyInfo FrameTimeMsProperty;

    public const string SampleCountPropertyName = "sampleCount";
    public readonly PropertyInfo SampleCountProperty;

    public const string ThreadNamePropertyName = "threadName";
    public readonly PropertyInfo ThreadNameProperty;
    
    public const string ThreadIndexPropertyName = "threadIndex";
    public readonly PropertyInfo ThreadIndexProperty;
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