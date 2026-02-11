#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Rider.Model.Unity;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;
using UnityEditorInternal;

namespace JetBrains.Rider.Unity.Editor.Profiler.SnapshotAnalysis
{
  internal class ProfilerSnapshotCrawler
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(ProfilerSnapshotCrawler));
    private readonly IProfilerSnapshotDriverAdapter? myProfilerSnapshotDriverAdapter;

    public event Action? ProfileLoaded;
    public event Action? ProfileCleared;

    public ProfilerSnapshotCrawler(IProfilerSnapshotDriverAdapter? profilerSnapshotDriverAdapter)
    {
      myProfilerSnapshotDriverAdapter = profilerSnapshotDriverAdapter;
      
      if (myProfilerSnapshotDriverAdapter != null)
      {
        myProfilerSnapshotDriverAdapter.ProfileLoaded += OnProfileLoaded;
        myProfilerSnapshotDriverAdapter.ProfileCleared += OnProfileCleared;
      }
    }

    private void OnProfileLoaded()
    {
      ourLogger.Verbose("ProfilerDriver.profileLoaded event received");
      ProfileLoaded?.Invoke();
    }

    private void OnProfileCleared()
    {
      ourLogger.Verbose("ProfilerDriver.profileCleared event received");
      ProfileCleared?.Invoke();
    }

    public int GetProfilerRecordFirstFrameIndex()
    {
      return myProfilerSnapshotDriverAdapter?.FirstFrameIndex ?? -1;
    }
    public int GetProfilerRecordLastFrameIndex()
    {
      return myProfilerSnapshotDriverAdapter?.LastFrameIndex ?? -1;
    }

    public UnityProfilerRecordInfo? GetProfilerRecordInfo()
    {
      var firstFrameIndex = GetProfilerRecordFirstFrameIndex();
      if(firstFrameIndex == -1)
        return null;
      var lastFrameIndex = GetProfilerRecordLastFrameIndex();
      if(lastFrameIndex == -1)
        return null;
      
      using var firstRawData = myProfilerSnapshotDriverAdapter?.GetRawFrameDataView(firstFrameIndex, 0);
      
      if(firstRawData is not { Valid: true })
        return null;
      
      using var lastRawData = myProfilerSnapshotDriverAdapter?.GetRawFrameDataView(lastFrameIndex, 0);
      if(lastRawData is not { Valid: true })
        return null;
      
      return new UnityProfilerRecordInfo(firstFrameIndex, lastFrameIndex, firstRawData.FrameTimeNs, lastRawData.FrameTimeNs);
    }
    
    public List<TimingInfo>? GetProfilerFrameSamplesTiming(int firstFrameIndex, int lastFrameIndex)
    {
      ourLogger.Verbose(nameof(GetProfilerFrameSamplesTiming));
      if (myProfilerSnapshotDriverAdapter == null)
      {
        ourLogger.Verbose($"{nameof(GetProfilerFrameSamplesTiming)}: {nameof(myProfilerSnapshotDriverAdapter)} is null");
        return null;
      }


      ourLogger.Verbose($"{nameof(GetProfilerFrameSamplesTiming)}: {nameof(firstFrameIndex)}:{firstFrameIndex} {nameof(lastFrameIndex)}:{lastFrameIndex}");
      
      var data = new List<TimingInfo>(lastFrameIndex - firstFrameIndex + 1);
      myProfilerSnapshotDriverAdapter.CollectFrameMs(firstFrameIndex, lastFrameIndex, data);

      ourLogger.Verbose($"{nameof(GetProfilerFrameSamplesTiming)}: {nameof(data.Count)}:{data.Count}");
      return data;
    }

    public List<ProfilerThread>? GetProfilerThreads(int frameIndex)
    {
      ourLogger.Verbose(nameof(GetProfilerThreads));
      if (myProfilerSnapshotDriverAdapter == null)
      {
        ourLogger.Verbose($"{nameof(GetProfilerThreads)}: {nameof(myProfilerSnapshotDriverAdapter)} is null");
        return null;
      }

      var threads = new List<ProfilerThread>();
      myProfilerSnapshotDriverAdapter.CollectThreads(threads, frameIndex);
      return threads;
    }

    /// <summary>
    /// Fetches profiler snapshot asynchronously with simple progress reporting (0.0 to 1.0)
    /// </summary>
    public Task<UnityProfilerSnapshot?> GetUnityProfilerSnapshotAsync(
      ProfilerSnapshotRequest request,
      Lifetime lifetime,
      IProgress<float>? progress = null)
    {
      ourLogger.Verbose($"GetUnityProfilerSnapshotAsync: frame={request.FrameIndex}, thread={request.Thread.Index}");

      // Validate input
      if (request.FrameIndex < 0 || request.Thread.Index < 0)
      {
        ourLogger.Verbose($"GetUnityProfilerSnapshotAsync: invalid request - frame={request.FrameIndex}, thread={request.Thread.Index}");
        return Task.FromResult<UnityProfilerSnapshot?>(null);
      }

      // Get raw frame data
      using var rawFrameDataView = myProfilerSnapshotDriverAdapter?.GetRawFrameDataView(request.FrameIndex, request.Thread.Index);

      if (rawFrameDataView is not { Valid: true })
      {
        ourLogger.Verbose("GetUnityProfilerSnapshotAsync: rawFrameDataView is null or invalid");
        return Task.FromResult<UnityProfilerSnapshot?>(null);
      }

      var sampleCount = rawFrameDataView.SampleCount;
      if (sampleCount == 0)
      {
        ourLogger.Verbose("GetUnityProfilerSnapshotAsync: no samples available");
        return Task.FromResult<UnityProfilerSnapshot?>(null);
      }

      // Report initial progress
      progress?.Report(0.0f);

      // Pre-allocate collections
      var markerIdToName = new List<MarkerToNamePair>(sampleCount / 4);
      var samples = new List<SampleInfo>(sampleCount);
      
#if UNITY_2022_3_OR_NEWER
      var knownMarkerIds = new HashSet<int>(sampleCount / 4);
#else
      var knownMarkerIds = new HashSet<int>();
#endif

      // Calculate batch size for progress reporting (report every 5%)
      var batchSize = Math.Max(1, sampleCount / 20);

      // Process samples (start from 1, skip root frame sample)
      for (var i = 1; i < sampleCount; i++)
      {
        lifetime.ThrowIfNotAlive();

        // Extract sample data
        var markerId = rawFrameDataView.GetSampleMarkerId(i);
        var duration = rawFrameDataView.GetSampleTimeMs(i);
        var childrenCount = rawFrameDataView.GetSampleChildrenCount(i);
        var memoryAlloc = rawFrameDataView.GetAllocSize(i);

        samples.Add(new SampleInfo(duration, markerId, memoryAlloc, childrenCount));

        // Collect unique marker names
        if (!knownMarkerIds.Contains(markerId))
        {
          var markerName = rawFrameDataView.GetSampleName(i);
          knownMarkerIds.Add(markerId);
          markerIdToName.Add(new MarkerToNamePair(markerId, markerName));
        }

        // Report progress periodically
        if (i % batchSize == 0)
        {
          var progressValue = i / (float)sampleCount;
          progress?.Report(progressValue);
        }
      }

      // Final progress
      progress?.Report(1.0f);

      // Create snapshot
      var snapshot = new UnityProfilerSnapshot(
        request.FrameIndex,
        rawFrameDataView.FrameStartTimeMs,
        rawFrameDataView.FrameTimeMs,
        new ProfilerThread(rawFrameDataView.ThreadIndex, rawFrameDataView.ThreadName),
        markerIdToName,
        samples);

      ourLogger.Verbose($"GetUnityProfilerSnapshotAsync: snapshot ready - {samples.Count} samples, {markerIdToName.Count} unique markers");
      return Task.FromResult<UnityProfilerSnapshot?>(snapshot);
    }
  }
}
