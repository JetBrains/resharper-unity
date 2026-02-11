#nullable enable
using System;
using System.Collections.Generic;
using JetBrains.Rider.Model.Unity;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces
{
  public interface IProfilerSnapshotDriverAdapter
  {
    IRawFrameDataViewAdapter? GetRawFrameDataView(int frameIndex, int threadIndex);
    int FirstFrameIndex { get; }
    int LastFrameIndex { get; }
    void CollectFrameMs(int firstFrameIndex, int lastFrameIndex, List<TimingInfo> frameMs);
    void CollectThreads(List<ProfilerThread> threads, int frameIndex);
    event Action? ProfileLoaded;
    event Action? ProfileCleared;
  }
}