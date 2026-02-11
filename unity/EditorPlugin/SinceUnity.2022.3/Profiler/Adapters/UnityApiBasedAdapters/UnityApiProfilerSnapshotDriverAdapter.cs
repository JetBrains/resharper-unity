#nullable enable
using System;
using System.Collections.Generic;
using JetBrains.Rider.Model.Unity;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;
using UnityEditorInternal;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.UnityApiBasedAdapters
{
  public class UnityApiProfilerSnapshotDriverAdapter : IProfilerSnapshotDriverAdapter
  {
    public UnityApiProfilerSnapshotDriverAdapter()
    {
      ProfilerDriver.profileLoaded += OnProfileLoaded;
      ProfilerDriver.profileCleared += OnProfileCleared;
    }

    public IRawFrameDataViewAdapter? GetRawFrameDataView(int frameIndex, int threadIndex)
    {
      var rawFrameDataView = ProfilerDriver.GetRawFrameDataView(frameIndex, threadIndex);
      return new UnityApiRawFrameDataViewAdapter(rawFrameDataView);
    }
    
    public int FirstFrameIndex => ProfilerDriver.firstFrameIndex;
    public int LastFrameIndex => ProfilerDriver.lastFrameIndex;
    
    public void CollectFrameMs(int firstFrameIndex, int lastFrameIndex, List<TimingInfo> frameMs)
    {
      for (var i = firstFrameIndex; i <= lastFrameIndex; i++)
      {
        var frameIndex = i - firstFrameIndex;
        using var rawFrameDataView = ProfilerDriver.GetRawFrameDataView(i, 0);
        frameMs.Add(new TimingInfo(frameIndex, rawFrameDataView.valid ? rawFrameDataView.frameTimeMs : 0)); 
      }
    }

    public void CollectThreads(List<ProfilerThread> threads, int frameIndex)
    {
      int threadIdx = 0;
      while(true)
      {
        using  var rawFrameDataView = ProfilerDriver.GetRawFrameDataView(frameIndex, threadIdx);
        if (!rawFrameDataView.valid)
          break;
        var threadName = rawFrameDataView.threadName;
        threads.Add(new ProfilerThread(threadIdx, threadName));
        threadIdx++;
      }
    }

    public event Action? ProfileLoaded;
    public event Action? ProfileCleared;

    private void OnProfileLoaded() => ProfileLoaded?.Invoke();
    private void OnProfileCleared() => ProfileCleared?.Invoke();
  }
}