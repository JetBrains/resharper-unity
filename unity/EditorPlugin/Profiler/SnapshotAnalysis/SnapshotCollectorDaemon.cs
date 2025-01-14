#nullable enable
using System;
using System.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Rd.Base;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.Model.Unity;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.SnapshotAnalysis;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.SnapshotNavigation;
using UnityEditor;
using UnityEditorInternal;

namespace JetBrains.Rider.Unity.Editor.Profiler.SnapshotAnalysis
{
  public interface ISnapshotCollectorDaemon
  {
    void Update(EditorWindow? ourLastProfilerWindow);
    void Deinit();
    void Advise(Lifetime connectionLifetime, UnityProfilerModel model);
  }

  internal class SnapshotCollectorDaemon : ISnapshotCollectorDaemon
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(SnapshotCollectorDaemon));

    //reflection providers
    private readonly SnapshotReflectionDataProvider mySnapshotReflectionDataProvider;
    private readonly ReflectionDataProvider myReflectionDataProvider;
    private ProfilerWindowAdapter? myProfilerWindowAdapter;

    private readonly ProfilerSnapshotCrawler mySnapshotCrawler;

    private readonly ViewableProperty<UnityProfilerSnapshot?> myLastSnapshot = new(null);
    private readonly ViewableProperty<UnityProfilerSnapshotStatus?> myProfilerStatus = new(null);

    private readonly SequentialLifetimes mySequentialLifetimes;

    internal SnapshotCollectorDaemon(SnapshotReflectionDataProvider snapshotReflectionDataProvider,
      ReflectionDataProvider reflectionDataProvider, Lifetime appDomainLifetime)
    {
      mySequentialLifetimes = new SequentialLifetimes(appDomainLifetime);
      mySnapshotReflectionDataProvider = snapshotReflectionDataProvider;
      myReflectionDataProvider = reflectionDataProvider;
      mySnapshotCrawler = new ProfilerSnapshotCrawler(ProfilerSnapshotDriverAdapter.Create(mySnapshotReflectionDataProvider));

      // Update the status to "UpToDate" when a snapshot becomes ready
      myLastSnapshot.Advise(appDomainLifetime, snapshot =>
        ourLogger.Verbose(
          $"Set {nameof(myLastSnapshot)}: " +
          $"{nameof(snapshot.FrameIndex)}:{snapshot?.FrameIndex ?? -1} " +
          $"{nameof(snapshot.ThreadIndex)}:{snapshot?.ThreadIndex ?? -1} " +
          $"{nameof(snapshot.Samples)}:{snapshot?.Samples.Count ?? -1}"));

      myProfilerStatus.Advise(appDomainLifetime, status => ourLogger.Verbose($"Set {nameof(myProfilerStatus)}: {status}"));
    }

    void ISnapshotCollectorDaemon.Deinit()
    {
      ourLogger.Verbose("Deinit");
      mySequentialLifetimes.TerminateCurrent();
      myLastSnapshot.Set(null);
      myProfilerWindowAdapter = null;
    }

    //Multiple advise calls could be ((
    void ISnapshotCollectorDaemon.Advise(Lifetime connectionLifetime, UnityProfilerModel model)
    {
      ourLogger.Verbose("Advise");
      //if myLastSnapshot already exists - mark status as ready to fetch
      myProfilerStatus.Set(myLastSnapshot.Value.ToSnapshotStatus(SnapshotStatus.HasNewSnapshotDataToFetch));
      
      model.GetUnityProfilerSnapshot.Set(async (lifetime, request) =>
      {
        var fetchNewSnapshotData = await FetchNewSnapshotData(lifetime, request);
        myLastSnapshot.Set(fetchNewSnapshotData);
        myProfilerStatus.Set(fetchNewSnapshotData.ToSnapshotStatus(SnapshotStatus.SnapshotDataIsUpToDate));
        return fetchNewSnapshotData;
      });
      myProfilerStatus.Advise(connectionLifetime, status => model.ProfilerSnapshotStatus.Set(status));
    }
    
    void ISnapshotCollectorDaemon.Update(EditorWindow? lastKnownProfilerWindow)
    {
      //Cancel all fetching tasks if the Editor is playing or Profiler is recording
      if (ProfilerDriver.enabled && EditorApplication.isPlaying)
      {
        mySequentialLifetimes.TerminateCurrent();
        return;
      }
      
      if (mySnapshotReflectionDataProvider is not { IsCompatibleWithCurrentUnityVersion: true })
        return;

      var profilerWindowObject = (EditorWindow?)myProfilerWindowAdapter?.ProfilerWindowObject;
      
      //if the profiler window has changed - create adapter for a new window
      if (profilerWindowObject != lastKnownProfilerWindow)
      {
        ourLogger.Verbose($"Update {nameof(myProfilerWindowAdapter)} because of {nameof(profilerWindowObject)} change: {lastKnownProfilerWindow}");
        myProfilerWindowAdapter = ProfilerWindowAdapter.Create(lastKnownProfilerWindow, myReflectionDataProvider.ProfilerWindowReflectionData);
      }

      //if the profiler window is closed or destroyed - clear existing cached snapshot information
      if ((EditorWindow?)myProfilerWindowAdapter?.ProfilerWindowObject == null) 
      {
        mySequentialLifetimes.TerminateCurrent();
        myLastSnapshot.Set(null);
        myProfilerStatus.Set(myLastSnapshot.Value.ToSnapshotStatus(SnapshotStatus.HasNewSnapshotDataToFetch));
        return;
      }
        
      UpdateSnapshotStatus(myProfilerWindowAdapter);
    }

    private Task<UnityProfilerSnapshot?> FetchNewSnapshotData(Lifetime lifetime, ProfilerSnapshotRequest snapshotRequest)
    {
      ourLogger.Verbose("FetchNewSnapshotData:");
      if (myProfilerWindowAdapter == null)
      {
        ourLogger.Verbose("FetchNewSnapshotData: myProfilerWindowAdapter is null");
        return Task.FromResult<UnityProfilerSnapshot?>(null);
      }

      if (myLastSnapshot.Value != null && 
          myLastSnapshot.Value.FrameIndex == snapshotRequest.FrameIndex &&
          myLastSnapshot.Value.ThreadIndex == snapshotRequest.ThreadIndex)
      {
        ourLogger.Verbose("FetchNewSnapshotData: myLastSnapshot is the same as the requested one");
        myProfilerStatus.Value = myLastSnapshot.Value.ToSnapshotStatus(SnapshotStatus.SnapshotDataIsUpToDate);
        return Task.FromResult<UnityProfilerSnapshot?>(myLastSnapshot.Value);
      }

      return StartSnapshotFetchingTask(snapshotRequest, lifetime);
    }

    private void UpdateSnapshotStatus(ProfilerWindowAdapter profilerWindowAdapter)
    {
      if (myProfilerWindowAdapter == null)
        return;

      var selectedFrameIndex = profilerWindowAdapter.GetSelectedFrameIndex();

      if (selectedFrameIndex == -1)
        return;

      //if the same current statusInfo has the same index or the status is NoDataAvailable we're updating it
      if (myProfilerStatus.Value != null &&
          myProfilerStatus.Value.FrameIndex == selectedFrameIndex &&
          myProfilerStatus.Value.Status != SnapshotStatus.NoSnapshotDataAvailable)
        return;

      var currentProfilerSnapshotStatusInfo = mySnapshotCrawler.GetCurrentProfilerSnapshotStatusInfo(selectedFrameIndex, 0);
      myProfilerStatus.Set(currentProfilerSnapshotStatusInfo);
    }

    private Task<UnityProfilerSnapshot?> StartSnapshotFetchingTask(ProfilerSnapshotRequest snapshotRequest, Lifetime lifetime)
    {
      ourLogger.Verbose("StartSnapshotFetchingTask");
      var snapshotFetchingLifetime = lifetime.Intersect(mySequentialLifetimes.Next());
      // Start a new task
      return Task.Run(async () =>
      {
        try
        {
          var progress = new Progress<UnityProfilerSnapshotStatus>(snapshotStatus => snapshotFetchingLifetime.Execute(() => myProfilerStatus.Set(snapshotStatus)));

          var profilerFrameSnapshot = await mySnapshotCrawler.GetUnityProfilerSnapshot(snapshotRequest, snapshotFetchingLifetime, progress);
          snapshotFetchingLifetime.Execute(() => myLastSnapshot.Set(profilerFrameSnapshot));
          return profilerFrameSnapshot;
        }
        catch (OperationCanceledException)
        {
          ourLogger.Verbose($"Task {nameof(mySnapshotCrawler.GetUnityProfilerSnapshot)} was canceled");
          snapshotFetchingLifetime.Execute(() => myLastSnapshot.Set(null));
          return null;
        }
        catch (Exception ex)
        {
          ourLogger.Error($"Task {nameof(mySnapshotCrawler.GetUnityProfilerSnapshot)} failed with exception", ex);
          snapshotFetchingLifetime.Execute(() => myLastSnapshot.Set(null));
          return null;
        }
      }, snapshotFetchingLifetime);
    }
  }
}