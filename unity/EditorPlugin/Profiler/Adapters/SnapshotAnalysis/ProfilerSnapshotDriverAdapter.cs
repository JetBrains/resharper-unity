#nullable enable
using System;
using JetBrains.Diagnostics;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.SnapshotAnalysis
{
  internal sealed class ProfilerSnapshotDriverAdapter
  {
    private readonly SnapshotReflectionDataProvider myReflectionDataProvider;
    private static readonly ILog ourLogger = Log.GetLog(nameof(ProfilerSnapshotDriverAdapter));
    private readonly ProfilerSnapshotDriverReflectionData myReflectionData;

    public static ProfilerSnapshotDriverAdapter? Create(SnapshotReflectionDataProvider reflectionDataProvider)
    {
      if (!reflectionDataProvider.IsCompatibleWithCurrentUnityVersion)
      {
        ourLogger.Verbose($"{reflectionDataProvider.GetType().Name} is not compatible with the current Unity version.");
        return null;
      }

      return new ProfilerSnapshotDriverAdapter(reflectionDataProvider);
    }

    private ProfilerSnapshotDriverAdapter(SnapshotReflectionDataProvider reflectionDataProvider)
    {
      myReflectionDataProvider = reflectionDataProvider;
      myReflectionData = myReflectionDataProvider.MyProfilerSnapshotDriverReflectionData;
      if (!myReflectionData.IsValid())
        ourLogger.Verbose($"{myReflectionData.GetType().Name} is not valid.");
    }

    public RawFrameDataViewAdapter? GetRawFrameDataView(int frameIndex, int threadIndex)
    {
      try
      {
        var rawFrameDataViewObject = myReflectionData.GetRawFrameDataViewMethod.Invoke(null, new object[] { frameIndex, threadIndex });
        return RawFrameDataViewAdapter.Create(rawFrameDataViewObject, myReflectionDataProvider.MyRawFrameDataViewReflectionData);
      }
      catch (Exception e)
      {
        ourLogger.Error(e, "Failed to retrieve raw frame data view.");
        return null;
      }
    }
  }
}