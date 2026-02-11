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
    private const string FirstFrameIndexFieldName = "firstFrameIndex";
    private const string LastFrameIndexFieldName = "lastFrameIndex";
    private const string ProfileLoadedFieldName = "profileLoaded";
    private const string ProfileClearedFieldName = "profileCleared";
    public readonly MethodInfo? GetRawFrameDataViewMethod;
    public readonly FieldInfo? FirstFrameIndexField;
    public readonly FieldInfo? LastFrameIndexField;
    public readonly FieldInfo? ProfileLoadedField;
    public readonly FieldInfo? ProfileClearedField;
    private readonly Type? myProfilerDriverType;

    public ProfilerSnapshotDriverReflectionData()
    {
      myProfilerDriverType = Assembly.GetAssembly(typeof(EditorWindow)).GetType(ProfilerDriverTypeName);
      GetRawFrameDataViewMethod =
        myProfilerDriverType.GetMethod(GetRawFrameDataViewMethodName, BindingFlags.Static | BindingFlags.Public);
      FirstFrameIndexField = myProfilerDriverType.GetField(FirstFrameIndexFieldName, BindingFlags.Static | BindingFlags.Public);
      LastFrameIndexField = myProfilerDriverType.GetField(LastFrameIndexFieldName, BindingFlags.Static | BindingFlags.Public);
      ProfileLoadedField = myProfilerDriverType.GetField(ProfileLoadedFieldName, BindingFlags.Static | BindingFlags.Public);
      ProfileClearedField = myProfilerDriverType.GetField(ProfileClearedFieldName, BindingFlags.Static | BindingFlags.Public);
    }

    public bool IsValid()
    {
      return myProfilerDriverType != null
             && GetRawFrameDataViewMethod != null
             && FirstFrameIndexField != null
             && LastFrameIndexField != null;
    }
  }

  internal sealed class RawFrameDataViewReflectionData : IReflectionData
  {
    private const string RawFrameDataViewTypeName = "UnityEditor.Profiling.RawFrameDataView";
    private const string GetSampleTimeMsMethodName = "GetSampleTimeMs";
    private const string GetSampleMarkerIdMethodName = "GetSampleMarkerId";
    private const string GetSampleChildrenCountMethodName = "GetSampleChildrenCount";
    private const string GetSampleNameMethodName = "GetSampleName";
    private const string GetMarkerIdMethodName = "GetMarkerId";
    private const string GetSampleMetadataCountMethodName = "GetSampleMetadataCount";
    private const string GetMarkerMetadataInfoMethodName = "GetMarkerMetadataInfo";
    private const string GetSampleMetadataAsLongMethodName = "GetSampleMetadataAsLong";
    private const string FrameStartTimeMsPropertyName = "frameStartTimeMs";
    private const string FrameTimeMsPropertyName = "frameTimeMs";
    private const string FrameTimeNsPropertyName = "frameTimeNs";
    private const string SampleCountPropertyName = "sampleCount";
    private const string ThreadNamePropertyName = "threadName";
    private const string ThreadIndexPropertyName = "threadIndex";
    private const string ValidPropertyName = "valid";

    public readonly PropertyInfo FrameStartTimeMsProperty;
    public readonly PropertyInfo FrameTimeMsProperty;
    public readonly PropertyInfo FrameTimeNsProperty;
    public readonly MethodInfo GetSampleChildrenCountMethod;
    public readonly MethodInfo GetSampleMarkerIdMethod;
    public readonly MethodInfo GetSampleNameMethod;
    public readonly MethodInfo GetSampleTimeMsMethod;
    public readonly MethodInfo GetMarkerIdMethod;
    public readonly MethodInfo GetSampleMetadataCountMethod;
    public readonly MethodInfo GetMarkerMetadataInfoMethod;
    public readonly MethodInfo GetSampleMetadataAsLongMethod;
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

      // Methods for GetAllocSize
      GetMarkerIdMethod =
        RawFrameDataViewType.GetMethod(GetMarkerIdMethodName, BindingFlags.Instance | BindingFlags.Public);
      GetSampleMetadataCountMethod =
        RawFrameDataViewType.GetMethod(GetSampleMetadataCountMethodName, BindingFlags.Instance | BindingFlags.Public);
      GetMarkerMetadataInfoMethod =
        RawFrameDataViewType.GetMethod(GetMarkerMetadataInfoMethodName, BindingFlags.Instance | BindingFlags.Public);
      GetSampleMetadataAsLongMethod =
        RawFrameDataViewType.GetMethod(GetSampleMetadataAsLongMethodName, BindingFlags.Instance | BindingFlags.Public);

      FrameStartTimeMsProperty =
        RawFrameDataViewType.GetProperty(FrameStartTimeMsPropertyName, BindingFlags.Instance | BindingFlags.Public);
      FrameTimeMsProperty =
        RawFrameDataViewType.GetProperty(FrameTimeMsPropertyName, BindingFlags.Instance | BindingFlags.Public);
      FrameTimeNsProperty =
        RawFrameDataViewType.GetProperty(FrameTimeNsPropertyName, BindingFlags.Instance | BindingFlags.Public);
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
             && GetMarkerIdMethod != null
             && GetSampleMetadataCountMethod != null
             && GetMarkerMetadataInfoMethod != null
             && GetSampleMetadataAsLongMethod != null
             && FrameStartTimeMsProperty != null
             && FrameTimeMsProperty != null
             && FrameTimeNsProperty != null
             && SampleCountProperty != null
             && ThreadNameProperty != null
             && ThreadIndexProperty != null;
    }
  }
}
