#nullable enable
using JetBrains.Diagnostics;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters.SnapshotAnalysis;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters.SnapshotNavigation;
using JetBrains.Rider.Unity.Editor.Profiler.SnapshotNavigation;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters
{
  internal class ReflectionBasedAdaptersFactory : AbstractAdaptersFactory
  {
    private readonly SnapshotReflectionDataProvider mySnapshotReflectionDataProvider = new();

    public override IProfilerWindowSelectionDataProvider CreateProfilerWindowFacade()
    {
      return new ProfilerWindowFacade(MyReflectionDataProvider, this);
    }

    public override IProfilerWindowAdapter? CreateProfilerWindowAdapter(EditorWindow? profilerWindowObject)
    {
      var reflectionData = MyReflectionDataProvider.ProfilerWindowReflectionData;
      if (reflectionData == null)
      {
        OurLogger.Verbose($"{nameof(ReflectionDataProvider.ProfilerWindowReflectionData)} is null.");
        return null;
      }

      if (!reflectionData.IsValid())
      {
        OurLogger.Verbose($"{reflectionData.GetType().Name} is not valid.");
        return null;
      }

      if (profilerWindowObject == null)
      {
        OurLogger.Verbose($"{nameof(profilerWindowObject)} object is null");
        return null;
      }

      if (profilerWindowObject.GetType() != reflectionData.ProfilerWindowType)
      {
        OurLogger.Verbose($"Type '{ProfilerWindowReflectionData.UnityEditorProfilerWindowType}' is expected.");
        return null;
      }

      return new ProfilerWindowAdapter(profilerWindowObject, reflectionData, this,
        MyReflectionDataProvider.CPUProfilerModuleReflectionData);
    }

    public override IProfilerSnapshotDriverAdapter? CreateProfilerSnapshotDriverAdapter()
    {
      if (!mySnapshotReflectionDataProvider.IsCompatibleWithCurrentUnityVersion)
      {
        OurLogger.Verbose(
          $"{mySnapshotReflectionDataProvider.GetType().Name} is not compatible with the current Unity version.");
        return null;
      }

      return new ProfilerSnapshotDriverAdapter(mySnapshotReflectionDataProvider, this);
    }

    public override IProfilerWindowTypeChecker? CreateProfilerWindowTypeChecker()
    {
      return new ProfilerWindowTypeChecker(MyReflectionDataProvider.ProfilerWindowReflectionData);
    }


    internal IRawFrameDataViewAdapter? CreateRawFrameDataViewAdapter(object? rawFrameDataViewObject)
    {
      var reflectionData = mySnapshotReflectionDataProvider.MyRawFrameDataViewReflectionData;

      if (reflectionData == null)
      {
        OurLogger.Verbose($"{nameof(mySnapshotReflectionDataProvider.MyRawFrameDataViewReflectionData)} is null.");
        return null;
      }

      if (!reflectionData.IsValid())
      {
        OurLogger.Verbose($"{reflectionData.GetType().Name} is not valid.");
        return null;
      }

      if (rawFrameDataViewObject == null)
      {
        OurLogger.Verbose($"{reflectionData.GetType().Name} object is null");
        return null;
      }

      return new RawFrameDataViewAdapter(rawFrameDataViewObject, reflectionData);
    }
  }
}