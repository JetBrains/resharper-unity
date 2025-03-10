#nullable enable
using System;
using JetBrains.Diagnostics;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters.SnapshotAnalysis
{
  internal sealed class ProfilerSnapshotDriverAdapter : IProfilerSnapshotDriverAdapter
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(ProfilerSnapshotDriverAdapter));
    private readonly ReflectionBasedAdaptersFactory myReflectionBasedAdaptersFactory;
    private readonly ProfilerSnapshotDriverReflectionData? myReflectionData;

    internal ProfilerSnapshotDriverAdapter(SnapshotReflectionDataProvider reflectionDataProvider,
      ReflectionBasedAdaptersFactory reflectionBasedAdaptersFactory)
    {
      myReflectionBasedAdaptersFactory = reflectionBasedAdaptersFactory;
      myReflectionData = reflectionDataProvider.MyProfilerSnapshotDriverReflectionData;
      if (myReflectionData == null)
      {
        ourLogger.Verbose($"{nameof(ProfilerSnapshotDriverReflectionData)} is null.");
        return;
      }

      if (!myReflectionData.IsValid())
        ourLogger.Verbose($"{myReflectionData.GetType().Name} is not valid.");
    }

    public IRawFrameDataViewAdapter? GetRawFrameDataView(int frameIndex, int threadIndex)
    {
      try
      {
        var rawFrameDataViewObject =
          myReflectionData?.GetRawFrameDataViewMethod.Invoke(null, new object[] { frameIndex, threadIndex });
        return myReflectionBasedAdaptersFactory.CreateRawFrameDataViewAdapter(rawFrameDataViewObject);
      }
      catch (Exception e)
      {
        ourLogger.Error(e, "Failed to retrieve raw frame data view.");
        return null;
      }
    }
  }
}