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
    private readonly IProfilerSnapshotDriverAdapter myProfilerSnapshotDriverAdapter;

    public ProfilerSnapshotCrawler(IProfilerSnapshotDriverAdapter profilerSnapshotDriverAdapter)
    {
      myProfilerSnapshotDriverAdapter = profilerSnapshotDriverAdapter;
    }

    public UnityProfilerSnapshotStatus GetCurrentProfilerSnapshotStatusInfo(int selectedFrameIndex, int threadIndex)
    {
      ourLogger.Trace(
        $"GetCurrentProfilerSnapshotStatusInfo: {nameof(selectedFrameIndex)}:{selectedFrameIndex} {nameof(threadIndex)}:{threadIndex}");
      using var rawFrameDataView = myProfilerSnapshotDriverAdapter.GetRawFrameDataView(selectedFrameIndex, threadIndex);
      return rawFrameDataView.ToSnapshotStatus(selectedFrameIndex,
        rawFrameDataView?.SampleCount > 0
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
        myProfilerSnapshotDriverAdapter.GetRawFrameDataView(snapshotRequestInfo.FrameIndex,
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

      var currentProfilerFrameSnapshot = new UnityProfilerSnapshot(
        snapshotRequestInfo.FrameIndex,
        rawFrameDataView.FrameStartTimeMs,
        rawFrameDataView.FrameTimeMs,
        threadId, threadName,
        new List<MarkerToNamePair>(sampleCount),
        new List<SampleInfo>(sampleCount),
        snapshotStatus);

      lifetime.ThrowIfNotAlive();

      var knownMarkerIds = new HashSet<int>();
      var batchSize = sampleCount / 20;
      for (var i = 1; i < sampleCount; i++)
      {
        var duration = rawFrameDataView.GetSampleTimeMs(i);
        var markerId = rawFrameDataView.GetSampleMarkerId(i);
        var childrenCount = rawFrameDataView.GetSampleChildrenCount(i);
        var sampleInfo = new SampleInfo(duration, markerId, childrenCount);
        currentProfilerFrameSnapshot.Samples.Add(sampleInfo);

        if (i % batchSize == 0)
          progress?.Report(rawFrameDataView.ToSnapshotStatus(snapshotRequestInfo.FrameIndex,
            SnapshotStatus.SnapshotDataFetchingInProgress,
            i / (float)sampleCount));

        lifetime.ThrowIfNotAlive();

        if (knownMarkerIds.Contains(markerId))
          continue;

        var sampleName = rawFrameDataView.GetSampleName(i);
        knownMarkerIds.Add(markerId);
        currentProfilerFrameSnapshot.MarkerIdToName.Add(new MarkerToNamePair(markerId, sampleName));
      }

      ourLogger.Verbose($"GetUnityProfilerSnapshot: {nameof(currentProfilerFrameSnapshot)} is ready");
      return Task.FromResult<UnityProfilerSnapshot?>(currentProfilerFrameSnapshot);
    }
  }
}