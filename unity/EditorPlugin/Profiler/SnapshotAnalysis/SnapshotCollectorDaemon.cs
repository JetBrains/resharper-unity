#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Rd.Base;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.Model.Unity;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;
using JetBrains.Threading;
using UnityEditor;
using UnityEditorInternal;

namespace JetBrains.Rider.Unity.Editor.Profiler.SnapshotAnalysis
{
  internal class SnapshotCollectorDaemon : ISnapshotCollectorDaemon
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(SnapshotCollectorDaemon));

    private readonly ProfilerSnapshotCrawler mySnapshotCrawler;
    private readonly IProfilerAdaptersFactory myAdaptersFactory;
    private readonly SequentialLifetimes myTimingsAndThreadsFetchLifetimes;

    private readonly ViewableProperty<UnityProfilerRecordInfo?> myRecordInfo = new(null);
    private readonly ViewableProperty<SelectionState?> mySelection = new(null);
    private readonly ViewableProperty<MainFrameTimingsAndThreads?> myTimingsAndThreads = new(null);

    private IProfilerWindowAdapter? myWindowAdapter;

    internal SnapshotCollectorDaemon(IProfilerAdaptersFactory adaptersFactory, Lifetime appDomainLifetime)
    {
      myAdaptersFactory = adaptersFactory;
      mySnapshotCrawler = new ProfilerSnapshotCrawler(adaptersFactory.CreateProfilerSnapshotDriverAdapter());
      myTimingsAndThreadsFetchLifetimes = new SequentialLifetimes(appDomainLifetime);
      
      mySnapshotCrawler.ProfileLoaded += OnProfileLoaded;
      mySnapshotCrawler.ProfileCleared += OnProfileCleared;
      appDomainLifetime.TryOnTermination(() => mySnapshotCrawler.ProfileLoaded -= OnProfileLoaded);
      appDomainLifetime.TryOnTermination(() => mySnapshotCrawler.ProfileCleared -= OnProfileCleared);
      
    }

    private void OnProfileLoaded()
    {
      ourLogger.Verbose("Profile loaded - updating record info, selection, and timings");
      UpdateRecordInfoFromProfiler();
      UpdateSelectionFromWindow();
      FetchTimingsAndThreadsAsync().NoAwait();
    }

    private void OnProfileCleared()
    {
      ourLogger.Verbose("Profile cleared - resetting state");
      myTimingsAndThreadsFetchLifetimes.TerminateCurrent();
      myRecordInfo.Set(null);
      mySelection.Set(null);
      myTimingsAndThreads.Set(null);
    }

    void ISnapshotCollectorDaemon.Deinit()
    {
      ourLogger.Verbose("Deinit");
      myTimingsAndThreadsFetchLifetimes.TerminateCurrent();
      myWindowAdapter = null;
      myRecordInfo.Set(null);
      mySelection.Set(null);
      myTimingsAndThreads.Set(null);
    }

    void ISnapshotCollectorDaemon.Advise(Lifetime connectionLifetime, UnityProfilerModel model)
    {
      ourLogger.Verbose("Advise");

      // Push local state to backend model
      myRecordInfo.Advise(connectionLifetime, record => model.CurrentProfilerRecordInfo.Set(record));
      mySelection.Advise(connectionLifetime, state => model.SelectionState.Set(state));
      myTimingsAndThreads.Advise(connectionLifetime, data => model.MainThreadTimingsAndThreads.Set(data));
      
      // Handle snapshot requests with reactive task
      model.RequestFrameSnapshot.Set((lifetime, request) =>
      {
        var task = new SnapshotRequestTask();
        task.FrameIndex.Set(request.FrameIndex);
        task.Thread.Set(request.Thread);
        task.Progress.Set(0.0f);
        task.Status.Set(JetBrains.Rider.Model.Unity.BackendUnity.TaskStatus.Running);
        task.ErrorMessage.Set(null);
        task.Snapshot.Set(null);
        
        StartSnapshotFetchTask(lifetime, request, task);
        
        return RdTask<SnapshotRequestTask>.Successful(task);
      });

      // Allow backend to update selection
      model.SelectionState.Advise(connectionLifetime, state =>
      {
        if (myWindowAdapter == null || state == null)
        {
          mySelection.Set(null);
          return;
        }

        var requestedFrame = state.SelectedFrameIndex;

        if (!IsFrameInValidRange(requestedFrame, out var firstFrame, out var lastFrame))
        {
          mySelection.Set(null);

          if (firstFrame == -1 || lastFrame == -1)
            ourLogger.Verbose($"Backend requested frame {requestedFrame}, but no profiler data available");
          else
            ourLogger.Verbose($"Backend requested frame {requestedFrame} outside valid range [{firstFrame}, {lastFrame}]");
          return;
        }

        myWindowAdapter.SetSelectedFrameIndex(requestedFrame);
        myWindowAdapter.SetSelectedThread(state.SelectedThread.Index);
        mySelection.Set(state);
        ourLogger.Verbose($"Backend requested frame selection: {requestedFrame}");
      });
    }

    void ISnapshotCollectorDaemon.Update(EditorWindow? profilerWindow)
    {
      // Early exit: don't process during play mode or active recording
      if (EditorApplication.isPlaying || (ProfilerDriver.enabled && ProfilerDriver.profileEditor))
      {
        myTimingsAndThreadsFetchLifetimes.TerminateCurrent();
        myRecordInfo.Set(null);
        mySelection.Set(null);
        myTimingsAndThreads.Set(null);
        return;
      }

      // Track profiler window changes
      if (HasProfilerWindowChanged(profilerWindow))
      {
        myWindowAdapter = myAdaptersFactory.CreateProfilerWindowAdapter(profilerWindow);
      }
      
      UpdateSelectionFromWindow();

      // Update record info and timings if profiler has data
      if (myWindowAdapter?.ProfilerWindowObject != null)
      {
        if (myRecordInfo.Value == null)
          UpdateRecordInfoFromProfiler();
        
        if (myTimingsAndThreads.Value == null)
          FetchTimingsAndThreadsAsync().NoAwait();
      }
    }

    private bool HasProfilerWindowChanged(EditorWindow? profilerWindow)
    {
      return myWindowAdapter?.ProfilerWindowObject as EditorWindow != profilerWindow;
    }

    private void UpdateSelectionFromWindow()
    {
      if (myWindowAdapter == null || myWindowAdapter.ProfilerWindowObject as EditorWindow == null)
      {
        mySelection.Set(null);
        return;
      }

      var selectedFrame = myWindowAdapter.GetSelectedFrameIndex();

      if (!IsFrameInValidRange(selectedFrame, out var firstFrame, out var lastFrame))
      {
        mySelection.Set(null);

        if (firstFrame == -1 || lastFrame == -1)
          ourLogger.Verbose($"Selected frame {selectedFrame}, but no profiler data available");
        else
          ourLogger.Verbose($"Selected frame {selectedFrame} is outside valid range [{firstFrame}, {lastFrame}]");

        return;
      }

      var selectedThreadId = myWindowAdapter.GetSelectedThreadId();
      if (selectedThreadId == -1)
      {
        mySelection.Set(null);
        ourLogger.Verbose($"Profiler window selection updated: frame {selectedFrame}, no thread selected");
        return;
      }
      
      if(mySelection.Value != null && mySelection.Value.SelectedFrameIndex == selectedFrame && mySelection.Value.SelectedThread.Index == selectedThreadId)
        return;

      var threads = mySnapshotCrawler.GetProfilerThreads(selectedFrame);
      var profilerThread = threads?.First(t => t.Index == selectedThreadId);

      mySelection.Set(new SelectionState(selectedFrame, profilerThread!));
      ourLogger.Verbose($"Profiler window selection updated: frame {selectedFrame}");
    }

    private bool IsFrameInValidRange(int frameIndex, out int firstFrame, out int lastFrame)
    {
      firstFrame = mySnapshotCrawler.GetProfilerRecordFirstFrameIndex();
      lastFrame = mySnapshotCrawler.GetProfilerRecordLastFrameIndex();

      return firstFrame != -1 && lastFrame != -1 && frameIndex >= firstFrame && frameIndex <= lastFrame;
    }

    private void UpdateRecordInfoFromProfiler()
    {
      var firstFrame = mySnapshotCrawler.GetProfilerRecordFirstFrameIndex();
      if (firstFrame == -1)
        return;

      var recordInfo = mySnapshotCrawler.GetProfilerRecordInfo();
      myRecordInfo.Set(recordInfo);

      ourLogger.Verbose($"Profiler record info updated: {recordInfo}");
    }

    private Task FetchTimingsAndThreadsAsync()
    {
      var firstFrame = mySnapshotCrawler.GetProfilerRecordFirstFrameIndex();
      var lastFrame = mySnapshotCrawler.GetProfilerRecordLastFrameIndex();
      
      if (firstFrame == -1 || lastFrame == -1)
      {
        myTimingsAndThreads.Set(null);
        ourLogger.Verbose($"Profiler record info not available, timings and threads not fetched: [{firstFrame}, {lastFrame}]");
        return Task.CompletedTask;
      }

      ourLogger.Verbose($"FetchTimingsAndThreadsAsync: [{firstFrame}, {lastFrame}]");
      
      var fetchLifetime = myTimingsAndThreadsFetchLifetimes.Next();

      try
      {
        return Task.Run(() =>
        {
          try
          {
            fetchLifetime.ThrowIfNotAlive();
            
            var threads = mySnapshotCrawler.GetProfilerThreads(firstFrame);
            fetchLifetime.ThrowIfNotAlive();
            
            var timings = mySnapshotCrawler.GetProfilerFrameSamplesTiming(firstFrame, lastFrame);
            fetchLifetime.ThrowIfNotAlive();

            if (fetchLifetime.IsAlive)
            {
              fetchLifetime.Execute(() =>
              {
                if (threads != null && threads.Count > 0 && timings != null && timings.Count > 0)
                {
                  myTimingsAndThreads.Set(new MainFrameTimingsAndThreads(timings, threads));
                  ourLogger.Verbose($"Timings and threads updated: {threads.Count} threads, {timings.Count} frames");
                }
                else
                {
                  myTimingsAndThreads.Set(null);
                }
              });
            }
          }
          catch (LifetimeCanceledException)
          {
            ourLogger.Verbose("FetchTimingsAndThreadsAsync was canceled");
          }
          catch (Exception ex)
          {
            ourLogger.Error("FetchTimingsAndThreadsAsync failed", ex);
            if (fetchLifetime.IsAlive)
              myTimingsAndThreads.Set(null);
          }
        }, fetchLifetime);
      }
      catch (LifetimeCanceledException)
      {
        ourLogger.Verbose("FetchTimingsAndThreadsAsync was canceled before starting");
      }

      return Task.CompletedTask;
    }

    private void StartSnapshotFetchTask(Lifetime lifetime, ProfilerSnapshotRequest request, SnapshotRequestTask task)
    {
      ourLogger.Verbose($"StartSnapshotFetchTask: frame {request.FrameIndex}, thread {request.Thread.Index}");

      if (myWindowAdapter == null)
      {
        ourLogger.Verbose("StartSnapshotFetchTask: window adapter is null");
        task.Status.Set(JetBrains.Rider.Model.Unity.BackendUnity.TaskStatus.Failed);
        task.ErrorMessage.Set("Profiler window adapter is not available");
        task.Snapshot.Set(null);
        return;
      }

      if (!IsFrameInValidRange(request.FrameIndex, out _, out _))
      {
        ourLogger.Verbose($"StartSnapshotFetchTask: frame {request.FrameIndex} is outside valid range");
        task.Status.Set(JetBrains.Rider.Model.Unity.BackendUnity.TaskStatus.Failed);
        task.ErrorMessage.Set($"Frame {request.FrameIndex} is outside valid range");
        task.Snapshot.Set(null);
        return;
      }

      // Create progress reporter that updates the task's Progress property
      var progress = new Progress<float>(progressValue =>
      {
        if (lifetime.IsAlive)
        {
          task.Progress.Set(progressValue);
          ourLogger.Trace($"Snapshot fetch progress: {progressValue:P0}");
        }
      });

      // Start background task
      Task.Run(async () =>
      {
        try
        {
          var snapshot = await mySnapshotCrawler.GetUnityProfilerSnapshotAsync(request, lifetime, progress);
          
          if (lifetime.IsAlive)
          {
            task.Snapshot.Set(snapshot);
            task.Progress.Set(1.0f);
            task.Status.Set(JetBrains.Rider.Model.Unity.BackendUnity.TaskStatus.Completed);
            ourLogger.Verbose($"Snapshot fetched: frame {snapshot?.FrameIndex}, {snapshot?.Samples.Count} samples");
          }
        }
        catch (LifetimeCanceledException)
        {
          ourLogger.Verbose("StartSnapshotFetchTask was canceled (lifetime)");
          if (lifetime.IsAlive)
          {
            task.Status.Set(JetBrains.Rider.Model.Unity.BackendUnity.TaskStatus.Cancelled);
            task.ErrorMessage.Set("Operation was cancelled");
            task.Snapshot.Set(null);
          }
        }
        catch (OperationCanceledException)
        {
          ourLogger.Verbose("StartSnapshotFetchTask was canceled (operation)");
          if (lifetime.IsAlive)
          {
            task.Status.Set(JetBrains.Rider.Model.Unity.BackendUnity.TaskStatus.Cancelled);
            task.ErrorMessage.Set("Operation was cancelled");
            task.Snapshot.Set(null);
          }
        }
        catch (Exception ex)
        {
          ourLogger.Error("StartSnapshotFetchTask failed", ex);
          if (lifetime.IsAlive)
          {
            task.Status.Set(JetBrains.Rider.Model.Unity.BackendUnity.TaskStatus.Failed);
            task.ErrorMessage.Set(ex.Message);
            task.Snapshot.Set(null);
          }
        }
      }, lifetime).NoAwait();
    }
  }
}