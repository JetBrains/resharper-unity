#nullable enable
using System;
using System.Collections.Generic;
using JetBrains.Diagnostics;
using JetBrains.Rider.Model.Unity;
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

      SubscribeToProfilerDriverEvents();
    }

    private void SubscribeToProfilerDriverEvents()
    {
      try
      {
        if (myReflectionData?.ProfileLoadedField != null)
        {
          var eventHandlerType = myReflectionData.ProfileLoadedField.EventHandlerType;
          var handler = Delegate.CreateDelegate(eventHandlerType, this, nameof(OnProfileLoaded));
          myReflectionData.ProfileLoadedField.AddEventHandler(null, handler);
        }

        if (myReflectionData?.ProfileClearedField != null)
        {
          var eventHandlerType = myReflectionData.ProfileClearedField.EventHandlerType;
          var handler = Delegate.CreateDelegate(eventHandlerType, this, nameof(OnProfileCleared));
          myReflectionData.ProfileClearedField.AddEventHandler(null, handler);
        }
      }
      catch (Exception e)
      {
        ourLogger.Error(e, "Failed to subscribe to ProfilerDriver events.");
      }
    }

    public IRawFrameDataViewAdapter? GetRawFrameDataView(int frameIndex, int threadIndex)
    {
      try
      {
        var rawFrameDataViewObject =
          myReflectionData?.GetRawFrameDataViewMethod?.Invoke(null, new object[] { frameIndex, threadIndex });
        return myReflectionBasedAdaptersFactory.CreateRawFrameDataViewAdapter(rawFrameDataViewObject);
      }
      catch (Exception e)
      {
        ourLogger.Error(e, "Failed to retrieve raw frame data view.");
        return null;
      }
    }

    public int FirstFrameIndex => (int)(myReflectionData?.FirstFrameIndexField?.GetValue(null) ?? -1);
    public int LastFrameIndex => (int)(myReflectionData?.LastFrameIndexField?.GetValue(null) ?? -1);

    public void CollectFrameMs(int firstFrameIndex, int lastFrameIndex, List<TimingInfo> frameMs)
    {
      for (var i = firstFrameIndex; i <= lastFrameIndex; i++)
      {
        var frameIndex = i - firstFrameIndex;
        using var rawFrameDataView = GetRawFrameDataView(i, 0);
        frameMs.Add(new TimingInfo(frameIndex, rawFrameDataView?.Valid == true ? rawFrameDataView.FrameTimeMs : 0));
      }
    }
    
    public void CollectThreads(List<ProfilerThread> threads, int frameIndex)
    {
      int threadIdx = 0;
      while (true)
      {
        using var rawFrameDataView = GetRawFrameDataView(frameIndex, threadIdx);
        if (rawFrameDataView == null || !rawFrameDataView.Valid)
          break;
        threads.Add(new ProfilerThread(threadIdx, rawFrameDataView.ThreadName));
        threadIdx++;
      }
    }

    public event Action? ProfileLoaded;
    public event Action? ProfileCleared;

    private void OnProfileLoaded() => ProfileLoaded?.Invoke();
    private void OnProfileCleared() => ProfileCleared?.Invoke();
  }
}