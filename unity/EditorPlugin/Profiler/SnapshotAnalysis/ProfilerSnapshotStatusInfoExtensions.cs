#nullable enable
using JetBrains.Rider.Model.Unity;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;

namespace JetBrains.Rider.Unity.Editor.Profiler.SnapshotAnalysis
{
  internal static class ProfilerSnapshotStatusInfoExtensions
  {
    private static readonly UnityProfilerSnapshotStatus ourUnavailableStatusInfo =
      new(-1, -1, string.Empty, -1, SnapshotStatus.NoSnapshotDataAvailable, 0f);

    public static UnityProfilerSnapshotStatus ToSnapshotStatus(this UnityProfilerSnapshot? profilerFrameSnapshot,
      SnapshotStatus snapshotStatus)
    {
      return profilerFrameSnapshot == null
        ? ourUnavailableStatusInfo
        : new UnityProfilerSnapshotStatus(
          profilerFrameSnapshot.FrameIndex,
          profilerFrameSnapshot.ThreadIndex, profilerFrameSnapshot.ThreadName,
          profilerFrameSnapshot.Samples.Count, snapshotStatus, 1f);
    }

    public static UnityProfilerSnapshotStatus ToSnapshotStatus(this IRawFrameDataViewAdapter? rawFrameDataView,
      int frameIndex, SnapshotStatus snapshotStatus, float progress = 0f)
    {
      if (rawFrameDataView == null)
        return ourUnavailableStatusInfo;

      return new UnityProfilerSnapshotStatus(frameIndex, rawFrameDataView.ThreadIndex,
        rawFrameDataView.ThreadName, rawFrameDataView.SampleCount, snapshotStatus, progress);
    }
  }
}