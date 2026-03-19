#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Rider.Model.Unity;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;

namespace JetBrains.Rider.Unity.Editor.Profiler.SnapshotAnalysis
{
  internal class McpProfilerAnalyzer
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(McpProfilerAnalyzer));
    private readonly IProfilerSnapshotDriverAdapter myAdapter;

    public McpProfilerAnalyzer(IProfilerSnapshotDriverAdapter adapter)
    {
      myAdapter = adapter;
    }

    public McpOverviewResponse? GetOverview(McpOverviewRequest request, Lifetime lifetime)
    {
      ourLogger.Verbose($"GetOverview: threshold={request.ThresholdMs}, limit={request.Limit}, sortBy={request.SortBy}");

      var firstFrame = myAdapter.FirstFrameIndex;
      var lastFrame = myAdapter.LastFrameIndex;
      if (firstFrame == -1 || lastFrame == -1)
        return null;

      var totalFrames = lastFrame - firstFrame + 1;
      var allDurations = new List<double>(totalFrames);
      var frames = new List<(int frameId, double durationMs, long allocBytes)>(totalFrames);
      var threshold = request.ThresholdMs;

      for (var i = firstFrame; i <= lastFrame; i++)
      {
        lifetime.ThrowIfNotAlive();

        using var rawData = myAdapter.GetRawFrameDataView(i, 0);
        if (rawData is not { Valid: true })
          continue;

        var durationMs = (double)rawData.FrameTimeMs;
        allDurations.Add(durationMs);

        if (threshold > 0 && durationMs < threshold)
          continue;

        long totalAlloc = 0;
        var sampleCount = rawData.SampleCount;
        for (var s = 0; s < sampleCount; s++)
          totalAlloc += rawData.GetAllocSize(s);

        frames.Add((i, durationMs, totalAlloc));
      }

      if (allDurations.Count == 0)
        return null;

      allDurations.Sort();
      var averageMs = allDurations.Average();
      var p50Ms = Percentile(allDurations, 0.50);
      var p95Ms = Percentile(allDurations, 0.95);
      var p99Ms = Percentile(allDurations, 0.99);

      switch (request.SortBy)
      {
        case SortByTime:
          frames.Sort((a, b) => a.frameId.CompareTo(b.frameId));
          break;
        case SortByMemory:
          frames.Sort((a, b) => b.allocBytes.CompareTo(a.allocBytes));
          break;
        default:
          frames.Sort((a, b) => b.durationMs.CompareTo(a.durationMs));
          break;
      }

      var writeCount = Math.Min(frames.Count, request.Limit);

      var filePath = GetTempFilePath("overview");
      using (var writer = new StreamWriter(filePath))
      {
        writer.WriteLine("# UNITY PROFILER OVERVIEW v1");
        writer.WriteLine($"# Recording: {firstFrame}..{lastFrame}");
        writer.WriteLine($"# SortBy: {request.SortBy}");
        writer.WriteLine($"# Threshold: {request.ThresholdMs}");
        writer.WriteLine("frame_id\tduration_ms\talloc_bytes");
        for (var w = 0; w < writeCount; w++)
        {
          var (frameId, durationMs, allocBytes) = frames[w];
          writer.WriteLine($"{frameId}\t{durationMs:F2}\t{allocBytes}");
        }
      }

      return new McpOverviewResponse(
        filePath, totalFrames, firstFrame, writeCount,
        averageMs, p50Ms, p95Ms, p99Ms);
    }

    public McpFrameAnalysisResponse? GetFrameAnalysis(McpFrameAnalysisRequest request, Lifetime lifetime)
    {
      ourLogger.Verbose($"GetFrameAnalysis: frame={request.FrameIndex}, thread={request.ThreadName}, focusOn={request.FocusOn}");

      var threadIndex = ResolveThreadIndex(request.FrameIndex, request.ThreadName);

      using var rawData = myAdapter.GetRawFrameDataView(request.FrameIndex, threadIndex);
      if (rawData is not { Valid: true })
        return null;

      var frameDurationMs = (double)rawData.FrameTimeMs;
      var sampleCount = rawData.SampleCount;

      var samples = new List<SampleNode>(sampleCount);
      long totalAllocBytes = 0;

      for (var i = 1; i < sampleCount; i++)
      {
        lifetime.ThrowIfNotAlive();
        var name = rawData.GetSampleName(i);
        var duration = rawData.GetSampleTimeMs(i);
        var childrenCount = rawData.GetSampleChildrenCount(i);
        var memAlloc = rawData.GetAllocSize(i);
        totalAllocBytes += memAlloc;

        samples.Add(new SampleNode(
          CleanSampleName(name), duration,
          duration / frameDurationMs * 100.0,
          memAlloc, childrenCount));
      }

      ComputeDepthsAndPaths(samples);

      if (request.FocusOn != null)
      {
        var callstack = BuildFocusedCallstack(samples, request.FocusOn, request.Limit);
        return new McpFrameAnalysisResponse(
          request.FrameIndex, request.ThreadName, frameDurationMs,
          totalAllocBytes, sampleCount - 1,
          new List<McpHotspotEntry>(), callstack);
      }
      else
      {
        var hotspots = BuildHotspots(samples, request.SortBy, request.Limit);
        return new McpFrameAnalysisResponse(
          request.FrameIndex, request.ThreadName, frameDurationMs,
          totalAllocBytes, sampleCount - 1,
          hotspots, null);
      }
    }

    public McpBatchAnalyzeResponse? GetBatchAnalyze(McpBatchAnalyzeRequest request, Lifetime lifetime)
    {
      ourLogger.Verbose($"GetBatchAnalyze: start={request.StartFrame}, limit={request.Limit}, threshold={request.ThresholdMs}, snapshots={request.SnapshotLimit}");

      var firstFrame = request.StartFrame >= 0 ? request.StartFrame : myAdapter.FirstFrameIndex;
      var lastFrame = myAdapter.LastFrameIndex;
      if (firstFrame == -1 || lastFrame == -1)
        return null;

      var threadIndex = ResolveThreadIndex(firstFrame, request.ThreadName);

      var frameDurations = new List<(int frameId, double durationMs)>();
      var endFrame = Math.Min(lastFrame, firstFrame + request.Limit - 1);

      for (var i = firstFrame; i <= endFrame; i++)
      {
        lifetime.ThrowIfNotAlive();
        using var rawData = myAdapter.GetRawFrameDataView(i, 0);
        if (rawData is not { Valid: true })
          continue;
        var durationMs = (double)rawData.FrameTimeMs;
        if (request.ThresholdMs <= 0 || durationMs >= request.ThresholdMs)
          frameDurations.Add((i, durationMs));
      }

      var framesAnalyzed = frameDurations.Count;

      var targetFrames = frameDurations
        .OrderByDescending(f => f.durationMs)
        .Take(request.SnapshotLimit)
        .ToList();

      var aggregation = new Dictionary<string, Aggregator>();
      var nameCache = new Dictionary<int, string>();
      var snapshotsFetched = 0;
      double totalDurationMs = 0;
      long totalAllocBytes = 0;

      foreach (var (frameId, frameDuration) in targetFrames)
      {
        lifetime.ThrowIfNotAlive();

        using var rawData = myAdapter.GetRawFrameDataView(frameId, threadIndex);
        if (rawData is not { Valid: true })
          continue;

        snapshotsFetched++;
        totalDurationMs += frameDuration;

        var sampleCount = rawData.SampleCount;
        for (var s = 1; s < sampleCount; s++)
        {
          var markerId = rawData.GetSampleMarkerId(s);
          if (!nameCache.TryGetValue(markerId, out var name))
          {
            name = CleanSampleName(rawData.GetSampleName(s));
            nameCache[markerId] = name;
          }

          var duration = rawData.GetSampleTimeMs(s);
          var memAlloc = rawData.GetAllocSize(s);
          totalAllocBytes += memAlloc;

          if (duration < request.MinSampleDurationMs)
            continue;

          if (!aggregation.TryGetValue(name, out var agg))
          {
            agg = new Aggregator();
            aggregation[name] = agg;
          }

          agg.TotalDurationMs += duration;
          agg.CallCount++;
          agg.TotalMemoryBytes += memAlloc;
          if (duration > agg.MaxSingleMs)
            agg.MaxSingleMs = duration;
          agg.FrameIds.Add(frameId);
        }
      }

      var sorted = request.SortBy == SortByMemory
        ? aggregation.OrderByDescending(kv => kv.Value.TotalMemoryBytes)
        : aggregation.OrderByDescending(kv => kv.Value.TotalDurationMs);

      var hotspots = sorted.Select(kv => new McpCrossFrameHotspot(
        kv.Key,
        kv.Value.TotalDurationMs,
        kv.Value.CallCount,
        kv.Value.CallCount > 0 ? kv.Value.TotalDurationMs / kv.Value.CallCount : 0,
        kv.Value.MaxSingleMs,
        kv.Value.TotalMemoryBytes,
        kv.Value.FrameIds.Count
      )).ToList();

      var filePath = GetTempFilePath("analyze");
      using (var writer = new StreamWriter(filePath))
      {
        writer.WriteLine("# UNITY PROFILER ANALYSIS v1");
        writer.WriteLine($"# Recording: {firstFrame}..{lastFrame}");
        writer.WriteLine($"# Thread: {request.ThreadName}");
        writer.WriteLine($"# FramesAnalyzed: {framesAnalyzed}");
        writer.WriteLine($"# SnapshotsFetched: {snapshotsFetched}");
        writer.WriteLine($"# SortBy: {request.SortBy}");
        writer.WriteLine("qualified_name\ttotal_duration_ms\tcall_count\tavg_duration_ms\tmax_single_ms\ttotal_memory_bytes\tframes_present");
        foreach (var h in hotspots)
          writer.WriteLine($"{h.QualifiedName}\t{h.TotalDurationMs:F2}\t{h.CallCount}\t{h.AvgDurationMs:F2}\t{h.MaxSingleDurationMs:F2}\t{h.TotalMemoryBytes}\t{h.FramesPresent}");
      }

      var top5 = hotspots.Take(5).ToList();

      return new McpBatchAnalyzeResponse(
        filePath, framesAnalyzed, snapshotsFetched,
        request.ThreadName, totalDurationMs, totalAllocBytes, top5);
    }

    #region Helpers

    private const string SortByTime = "time";
    private const string SortByMemory = "memory";

    private int ResolveThreadIndex(int frameIndex, string threadName)
    {
      var threads = new List<ProfilerThread>();
      myAdapter.CollectThreads(threads, frameIndex);
      var match = threads.FirstOrDefault(t =>
        string.Equals(t.Name, threadName, StringComparison.OrdinalIgnoreCase));
      return match?.Index ?? 0;
    }

    private static string GetTempFilePath(string toolName)
    {
      var dir = Path.Combine(Path.GetTempPath(), "unity-profiler-mcp");
      Directory.CreateDirectory(dir);
      return Path.Combine(dir, $"{toolName}_{Guid.NewGuid():N}.tsv");
    }

    private static double Percentile(List<double> sorted, double p)
    {
      if (sorted.Count == 0) return 0;
      var index = p * (sorted.Count - 1);
      var lower = (int)Math.Floor(index);
      var upper = (int)Math.Ceiling(index);
      if (lower == upper) return sorted[lower];
      var weight = index - lower;
      return sorted[lower] * (1 - weight) + sorted[upper] * weight;
    }

    // Mirrors SamplesCacheUtils.ParseSampleName logic (different runtime, can't share code)
    private static string CleanSampleName(string name)
    {
      if (string.IsNullOrEmpty(name))
        return name;

      var excl = name.IndexOf('!');
      if (excl >= 0)
        name = name.Substring(excl + 1);

      name = name.Replace("::", ".");

      if (name.EndsWith(" [Invoke]"))
        name = name.Substring(0, name.Length - 9);
      if (name.EndsWith("()"))
        name = name.Substring(0, name.Length - 2);

      return name.Trim();
    }

    #endregion

    #region Internal types

    private sealed class SampleNode
    {
      public readonly string Name;
      public readonly double DurationMs;
      public readonly double FramePercentage;
      public readonly long MemoryBytes;
      public readonly int ChildrenCount;
      public int Depth;
      public int ParentIndex = -1;
      public string Path = "";

      public SampleNode(string name, double durationMs, double framePercentage, long memoryBytes, int childrenCount)
      {
        Name = name;
        DurationMs = durationMs;
        FramePercentage = framePercentage;
        MemoryBytes = memoryBytes;
        ChildrenCount = childrenCount;
      }
    }

    private sealed class HotspotAccumulator
    {
      public double TotalDurationMs;
      public int CallCount;
      public long TotalMemoryBytes;
      public string BestPath = "";
      public double BestDuration;
    }

    private sealed class Aggregator
    {
      public double TotalDurationMs;
      public int CallCount;
      public long TotalMemoryBytes;
      public double MaxSingleMs;
      public readonly HashSet<int> FrameIds = new();
    }

    #endregion

    #region Tree algorithms

    private static void ComputeDepthsAndPaths(List<SampleNode> samples)
    {
      var i = 0;
      var sb = new System.Text.StringBuilder(256);

      void Walk(int depth, int parentIdx)
      {
        if (i >= samples.Count) return;
        var sample = samples[i];
        sample.Depth = depth;
        sample.ParentIndex = parentIdx;

        var lengthBefore = sb.Length;
        if (sb.Length > 0) sb.Append('/');
        sb.Append(sample.Name);
        sample.Path = sb.ToString();

        var currentIdx = i;
        i++;

        for (var c = 0; c < sample.ChildrenCount; c++)
          Walk(depth + 1, currentIdx);

        sb.Length = lengthBefore;
      }

      while (i < samples.Count)
        Walk(0, -1);
    }

    private static List<McpHotspotEntry> BuildHotspots(List<SampleNode> samples, string sortBy, int limit)
    {
      var grouped = new Dictionary<string, HotspotAccumulator>();

      foreach (var s in samples)
      {
        if (!grouped.TryGetValue(s.Name, out var acc))
        {
          acc = new HotspotAccumulator();
          grouped[s.Name] = acc;
        }

        acc.TotalDurationMs += s.DurationMs;
        acc.CallCount++;
        acc.TotalMemoryBytes += s.MemoryBytes;
        if (s.DurationMs > acc.BestDuration)
        {
          acc.BestDuration = s.DurationMs;
          acc.BestPath = s.Path;
        }
      }

      var sorted = sortBy == SortByMemory
        ? grouped.OrderByDescending(kv => kv.Value.TotalMemoryBytes)
        : grouped.OrderByDescending(kv => kv.Value.TotalDurationMs);

      return sorted.Take(limit).Select(kv => new McpHotspotEntry(
        kv.Key, kv.Value.BestPath, kv.Value.TotalDurationMs,
        kv.Value.CallCount, kv.Value.TotalMemoryBytes
      )).ToList();
    }

    private static List<McpCallstackEntry> BuildFocusedCallstack(List<SampleNode> samples, string focusOn, int limit)
    {
      var isPathMatch = focusOn.Contains('/');
      var results = new List<McpCallstackEntry>();
      var addedPaths = new HashSet<string>();

      var matchIndices = new List<int>();
      for (var i = 0; i < samples.Count; i++)
      {
        var s = samples[i];
        var matches = isPathMatch
          ? string.Equals(s.Path, focusOn, StringComparison.Ordinal)
          : string.Equals(s.Name, focusOn, StringComparison.OrdinalIgnoreCase);
        if (matches)
          matchIndices.Add(i);
      }

      foreach (var matchIdx in matchIndices.Take(limit))
      {
        var target = samples[matchIdx];

        var ancestors = new List<int>();
        var pi = target.ParentIndex;
        while (pi >= 0)
        {
          ancestors.Add(pi);
          pi = samples[pi].ParentIndex;
        }
        ancestors.Reverse();

        foreach (var ai in ancestors)
        {
          var ancestor = samples[ai];
          if (addedPaths.Add(ancestor.Path))
          {
            results.Add(new McpCallstackEntry(
              ancestor.Name, ancestor.Path, ancestor.DurationMs,
              ancestor.FramePercentage, ancestor.MemoryBytes,
              ancestor.Depth, ancestor.ChildrenCount, false));
          }
        }

        if (addedPaths.Add(target.Path))
        {
          results.Add(new McpCallstackEntry(
            target.Name, target.Path, target.DurationMs,
            target.FramePercentage, target.MemoryBytes,
            target.Depth, target.ChildrenCount, true));
        }

        var ci = matchIdx + 1;
        var remaining = target.ChildrenCount;
        while (remaining > 0 && ci < samples.Count)
        {
          var child = samples[ci];
          if (child.Depth == target.Depth + 1)
          {
            results.Add(new McpCallstackEntry(
              child.Name, child.Path, child.DurationMs,
              child.FramePercentage, child.MemoryBytes,
              child.Depth, child.ChildrenCount, false));
          }
          remaining--;
          ci++;
          var skip = child.ChildrenCount;
          while (skip > 0 && ci < samples.Count)
          {
            skip += samples[ci].ChildrenCount - 1;
            ci++;
          }
        }
      }

      return results;
    }

    #endregion
  }
}