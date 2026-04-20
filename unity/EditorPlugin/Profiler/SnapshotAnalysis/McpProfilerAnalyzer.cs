#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Collections;
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

    public McpOverviewResponse? GetOverview(McpOverviewRequest request, Lifetime lifetime, int firstFrame, int lastFrame)
    {
      ourLogger.Verbose($"GetOverview: threshold={request.ThresholdMs}, limit={request.Limit}, sortBy={request.SortBy}, range=[{firstFrame},{lastFrame}]");

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
        case ProfilerSortingType.frameId:
          frames.Sort((a, b) => a.frameId.CompareTo(b.frameId));
          break;
        case ProfilerSortingType.memory:
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
        writer.WriteLine(FormattableString.Invariant($"# Threshold: {request.ThresholdMs}"));
        writer.WriteLine("frame_id\tduration_ms\talloc_bytes");
        for (var w = 0; w < writeCount; w++)
        {
          var (frameId, durationMs, allocBytes) = frames[w];
          writer.WriteLine(FormattableString.Invariant($"{frameId}\t{durationMs:F2}\t{allocBytes}"));
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

    public McpBatchAnalyzeResponse? GetBatchAnalyze(McpBatchAnalyzeRequest request, Lifetime lifetime, int firstFrame, int lastFrame)
    {
      ourLogger.Verbose($"GetBatchAnalyze: start={request.StartFrame}, limit={request.Limit}, threshold={request.ThresholdMs}, snapshots={request.SnapshotLimit}, range=[{firstFrame},{lastFrame}]");

      if (request.StartFrame >= 0)
        firstFrame = request.StartFrame;
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
        var count = sampleCount - 1;
        if (count <= 0) continue;

        // First pass: read all sample data
        var markerIds = new int[count];
        var durations = new double[count];
        var childrenCounts = new int[count];
        var allocs = new long[count];

        long frameSelfAlloc = 0;
        for (var s = 0; s < count; s++)
        {
          var raw = s + 1;
          markerIds[s] = rawData.GetSampleMarkerId(raw);
          durations[s] = rawData.GetSampleTimeMs(raw);
          childrenCounts[s] = rawData.GetSampleChildrenCount(raw);
          allocs[s] = rawData.GetAllocSize(raw);
          frameSelfAlloc += allocs[s];
          if (!nameCache.TryGetValue(markerIds[s], out _))
            nameCache[markerIds[s]] = CleanSampleName(rawData.GetSampleName(raw));
        }
        totalAllocBytes += frameSelfAlloc;

        // Propagate leaf allocations to parent functions
        PropagateAllocationsToParents(childrenCounts, allocs, count);

        // Second pass: aggregate using inclusive allocations
        for (var s = 0; s < count; s++)
        {
          if (durations[s] < request.MinSampleDurationMs)
            continue;

          var name = nameCache[markerIds[s]];
          if (!aggregation.TryGetValue(name, out var agg))
          {
            agg = new Aggregator();
            aggregation[name] = agg;
          }

          agg.TotalDurationMs += durations[s];
          agg.CallCount++;
          agg.TotalMemoryBytes += allocs[s];
          if (durations[s] > agg.MaxSingleMs)
            agg.MaxSingleMs = durations[s];
          agg.FrameIds.Add(frameId);
        }
      }

      var sorted = request.SortBy == ProfilerSortingType.memory
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
          writer.WriteLine(FormattableString.Invariant($"{h.QualifiedName}\t{h.TotalDurationMs:F2}\t{h.CallCount}\t{h.AvgDurationMs:F2}\t{h.MaxSingleDurationMs:F2}\t{h.TotalMemoryBytes}\t{h.FramesPresent}"));
      }

      var top5 = hotspots.Take(5).ToList();

      return new McpBatchAnalyzeResponse(
        filePath, framesAnalyzed, snapshotsFetched,
        request.ThreadName, totalDurationMs, totalAllocBytes, top5);
    }

    #region Helpers


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

    /// <summary>
    /// Propagates allocations from leaf GC.Alloc markers to parent functions.
    /// After this call, allocs[i] contains inclusive allocation (self + all descendants).
    /// Same approach as SamplesCacheUtils.ConstructCache / ShareMemoryAllocationsWithParent.
    /// </summary>
    private static void PropagateAllocationsToParents(int[] childrenCounts, long[] allocs, int count)
    {
      var stackIdx = new int[count];
      var stackRem = new int[count];
      var top = -1;

      for (var i = 0; i < count; i++)
      {
        // Pop completed parents, propagating their inclusive alloc upward
        while (top >= 0 && stackRem[top] == 0)
        {
          if (top > 0)
            allocs[stackIdx[top - 1]] += allocs[stackIdx[top]];
          top--;
        }

        // Current sample's parent is top of stack
        if (top >= 0)
        {
          stackRem[top]--;
          // Leaf: propagate directly to parent
          if (childrenCounts[i] == 0)
            allocs[stackIdx[top]] += allocs[i];
        }

        // Push if has children
        if (childrenCounts[i] > 0)
        {
          top++;
          stackIdx[top] = i;
          stackRem[top] = childrenCounts[i];
        }
      }

      // Unwind remaining stack
      while (top >= 0)
      {
        if (top > 0)
          allocs[stackIdx[top - 1]] += allocs[stackIdx[top]];
        top--;
      }
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
      public long InclusiveMemoryBytes;
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
        InclusiveMemoryBytes = memoryBytes;
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

        // Propagate inclusive alloc to parent (same as ShareMemoryAllocationsWithParent)
        if (parentIdx >= 0)
          samples[parentIdx].InclusiveMemoryBytes += sample.InclusiveMemoryBytes;

        sb.Length = lengthBefore;
      }

      while (i < samples.Count)
        Walk(0, -1);
    }

    private static List<McpHotspotEntry> BuildHotspots(List<SampleNode> samples, ProfilerSortingType sortBy, int limit)
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
        acc.TotalMemoryBytes += s.InclusiveMemoryBytes;
        if (s.DurationMs > acc.BestDuration)
        {
          acc.BestDuration = s.DurationMs;
          acc.BestPath = s.Path;
        }
      }

      var sorted = sortBy == ProfilerSortingType.memory
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

        // Ancestors: unique by path, callCount=1
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
              ancestor.FramePercentage, ancestor.InclusiveMemoryBytes,
              ancestor.Depth, ancestor.ChildrenCount, 1, false));
          }
        }

        // Target: callCount=1
        if (addedPaths.Add(target.Path))
        {
          results.Add(new McpCallstackEntry(
            target.Name, target.Path, target.DurationMs,
            target.FramePercentage, target.InclusiveMemoryBytes,
            target.Depth, target.ChildrenCount, 1, true));
        }

        // Children: aggregate by name
        var childAgg = new Dictionary<string, (double durationMs, double framePct, long memBytes, int maxChildren, int callCount)>();
        var ci = matchIdx + 1;
        var remaining = target.ChildrenCount;
        var childDepth = target.Depth + 1;
        while (remaining > 0 && ci < samples.Count)
        {
          var child = samples[ci];
          if (child.Depth == childDepth)
          {
            if (childAgg.TryGetValue(child.Name, out var prev))
            {
              childAgg[child.Name] = (
                prev.durationMs + child.DurationMs,
                prev.framePct + child.FramePercentage,
                prev.memBytes + child.InclusiveMemoryBytes,
                Math.Max(prev.maxChildren, child.ChildrenCount),
                prev.callCount + 1);
            }
            else
            {
              childAgg[child.Name] = (child.DurationMs, child.FramePercentage,
                child.InclusiveMemoryBytes, child.ChildrenCount, 1);
            }
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

        foreach (var (name, agg) in childAgg)
        {
          results.Add(new McpCallstackEntry(
            name, target.Path + "/" + name, agg.durationMs,
            agg.framePct, agg.memBytes,
            childDepth, agg.maxChildren, agg.callCount, false));
        }
      }

      return results;
    }

    #endregion
  }
}