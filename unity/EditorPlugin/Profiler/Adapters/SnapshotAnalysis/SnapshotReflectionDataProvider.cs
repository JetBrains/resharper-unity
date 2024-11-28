using System;
using JetBrains.Diagnostics;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.SnapshotNavigation;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.SnapshotAnalysis
{
  internal class SnapshotReflectionDataProvider
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(ReflectionDataProvider));
    public readonly bool IsCompatibleWithCurrentUnityVersion;
    internal readonly ProfilerSnapshotDriverReflectionData MyProfilerSnapshotDriverReflectionData;
    internal readonly RawFrameDataViewReflectionData MyRawFrameDataViewReflectionData;

    internal SnapshotReflectionDataProvider()
    {
      try
      {
        MyProfilerSnapshotDriverReflectionData = new ProfilerSnapshotDriverReflectionData();
        MyRawFrameDataViewReflectionData = new RawFrameDataViewReflectionData();
        if (!MyProfilerSnapshotDriverReflectionData.IsValid())
        {
          IsCompatibleWithCurrentUnityVersion = false;
          ourLogger.Verbose(
            $"The {nameof(ProfilerSnapshotDriverReflectionData)} is not compatible with the current Unity version.");
          return;
        }

        if (!MyRawFrameDataViewReflectionData.IsValid())
        {
          IsCompatibleWithCurrentUnityVersion = false;
          ourLogger.Verbose(
            $"The {nameof(RawFrameDataViewReflectionData)} is not compatible with the current Unity version.");
          return;
        }

        IsCompatibleWithCurrentUnityVersion = true;
      }
      catch (Exception e)
      {
        IsCompatibleWithCurrentUnityVersion = false;
        ourLogger.Verbose($"Exception while initializing {nameof(SnapshotReflectionDataProvider)}:  {e}");
      }
    }
  }
}