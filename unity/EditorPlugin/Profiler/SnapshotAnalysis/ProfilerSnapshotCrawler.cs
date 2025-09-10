#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Rider.Model.Unity;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;

namespace JetBrains.Rider.Unity.Editor.Profiler.SnapshotAnalysis
{
  internal class ProfilerSnapshotCrawler
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(ProfilerSnapshotCrawler));
    private readonly IProfilerSnapshotDriverAdapter? myProfilerSnapshotDriverAdapter;

    public ProfilerSnapshotCrawler(IProfilerSnapshotDriverAdapter? profilerSnapshotDriverAdapter)
    {
      myProfilerSnapshotDriverAdapter = profilerSnapshotDriverAdapter;
    }

    public UnityProfilerSnapshotStatus GetCurrentProfilerSnapshotStatusInfo(int selectedFrameIndex, int threadIndex)
    {
      ourLogger.Trace(
        $"GetCurrentProfilerSnapshotStatusInfo: {nameof(selectedFrameIndex)}:{selectedFrameIndex} {nameof(threadIndex)}:{threadIndex}");
      using var rawFrameDataView = myProfilerSnapshotDriverAdapter?.GetRawFrameDataView(selectedFrameIndex, threadIndex);

      return rawFrameDataView.ToSnapshotStatus(selectedFrameIndex,
       rawFrameDataView is { Valid: true, SampleCount: > 0 }
          ? SnapshotStatus.HasNewSnapshotDataToFetch
          : SnapshotStatus.NoSnapshotDataAvailable);
    }

    //No task needed
    public Task<UnityProfilerSnapshot?> GetUnityProfilerSnapshot(ProfilerSnapshotRequest snapshotRequestInfo,
      Lifetime lifetime, IProgress<UnityProfilerSnapshotStatus>? progress = null)
    {
      ourLogger.Verbose($"GetUnityProfilerSnapshot: {nameof(snapshotRequestInfo.FrameIndex)}:{snapshotRequestInfo.FrameIndex} {nameof(snapshotRequestInfo.ThreadIndex)}:{snapshotRequestInfo.ThreadIndex}");
      if (snapshotRequestInfo.FrameIndex < 0 || snapshotRequestInfo.ThreadIndex < 0)
      {
        ourLogger.Verbose($"GetUnityProfilerSnapshot: {nameof(snapshotRequestInfo.FrameIndex)}:{snapshotRequestInfo.FrameIndex} {nameof(snapshotRequestInfo.ThreadIndex)}:{snapshotRequestInfo.ThreadIndex} is invalid");
        return Task.FromResult<UnityProfilerSnapshot?>(null);
      }

      using var rawFrameDataView =
        myProfilerSnapshotDriverAdapter?.GetRawFrameDataView(snapshotRequestInfo.FrameIndex,
          snapshotRequestInfo.ThreadIndex);

      if (rawFrameDataView == null)
      {
        ourLogger.Verbose($"GetUnityProfilerSnapshot: {nameof(rawFrameDataView)} is null");
        return Task.FromResult<UnityProfilerSnapshot?>(null);
      }

      var sampleCount = rawFrameDataView.SampleCount;
      var threadName = rawFrameDataView.ThreadName;
      var threadId = rawFrameDataView.ThreadIndex;

      var snapshotStatus = rawFrameDataView.ToSnapshotStatus(snapshotRequestInfo.FrameIndex,
        SnapshotStatus.SnapshotDataFetchingInProgress);
      progress?.Report(snapshotStatus);

      // Pre-allocate collections with capacity to avoid resizing
      var markerIdToName = new List<MarkerToNamePair>(sampleCount / 4); // Assuming ~25% unique markers
      var samples = new List<SampleInfo>(sampleCount);

      var currentProfilerFrameSnapshot = new UnityProfilerSnapshot(
        snapshotRequestInfo.FrameIndex,
        rawFrameDataView.FrameStartTimeMs,
        rawFrameDataView.FrameTimeMs,
        threadId, threadName,
        markerIdToName,
        samples,
        snapshotStatus);

      lifetime.ThrowIfNotAlive();

      // Use HashSet for faster lookups
      var knownMarkerIds =
#if UNITY_2022_3_OR_NEWER
        new HashSet<int>(sampleCount / 4);
#else
        new HashSet<int>();
#endif
      
      var batchSize = Math.Max(1, sampleCount / 20); // Ensure batchSize is at least 1

      // Start from 1 as the first sample is usually the frame itself
      for (var i = 1; i < sampleCount; i++)
      {
        // Get all sample data at once to minimize method calls
        var markerId = rawFrameDataView.GetSampleMarkerId(i);
        var duration = rawFrameDataView.GetSampleTimeMs(i);
        var childrenCount = rawFrameDataView.GetSampleChildrenCount(i);
        var memoryAllocSize = rawFrameDataView.GetAllocSize(i);

        // Create and add sample info
        samples.Add(new SampleInfo(duration, markerId, memoryAllocSize, childrenCount));

        // Report progress periodically
        if (i % batchSize == 0)
          progress?.Report(rawFrameDataView.ToSnapshotStatus(snapshotRequestInfo.FrameIndex,
            SnapshotStatus.SnapshotDataFetchingInProgress,
            i / (float)sampleCount));

        lifetime.ThrowIfNotAlive();

        // Only get sample name if we haven't seen this marker ID before
        if (!knownMarkerIds.Contains(markerId))
        {
          var sampleName = rawFrameDataView.GetSampleName(i);
          knownMarkerIds.Add(markerId);
          markerIdToName.Add(new MarkerToNamePair(markerId, sampleName));
        }
      }

      ourLogger.Verbose($"GetUnityProfilerSnapshot: {nameof(currentProfilerFrameSnapshot)} is ready");
      return Task.FromResult<UnityProfilerSnapshot?>(currentProfilerFrameSnapshot);
    }
  }
}
