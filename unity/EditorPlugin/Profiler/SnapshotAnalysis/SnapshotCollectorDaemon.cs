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
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;
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
    private readonly IProfilerAdaptersFactory myAdaptersFactory;
    private readonly ViewableProperty<UnityProfilerSnapshot?> myLastSnapshot = new(null);
    private readonly ViewableProperty<UnityProfilerSnapshotStatus?> myProfilerStatus = new(null);
    private readonly SequentialLifetimes mySequentialLifetimes;
    private readonly ProfilerSnapshotCrawler mySnapshotCrawler;
    private IProfilerWindowAdapter? myProfilerWindowAdapter;

    internal SnapshotCollectorDaemon(IProfilerAdaptersFactory adaptersFactory, Lifetime appDomainLifetime)
    {
      myAdaptersFactory = adaptersFactory;
      mySequentialLifetimes = new SequentialLifetimes(appDomainLifetime);

      mySnapshotCrawler = new ProfilerSnapshotCrawler(myAdaptersFactory.CreateProfilerSnapshotDriverAdapter()!);

      // Update the status to "UpToDate" when a snapshot becomes ready
      myLastSnapshot.Advise(appDomainLifetime, snapshot =>
        ourLogger.Verbose(
          $"Set {nameof(myLastSnapshot)}: " +
          $"{nameof(snapshot.FrameIndex)}:{snapshot?.FrameIndex ?? -1} " +
          $"{nameof(snapshot.ThreadIndex)}:{snapshot?.ThreadIndex ?? -1} " +
          $"{nameof(snapshot.Samples)}:{snapshot?.Samples.Count ?? -1}"));

      myProfilerStatus.Advise(appDomainLifetime,
        status => ourLogger.Verbose($"Set {nameof(myProfilerStatus)}: {status}"));
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
      // Cancel all fetching tasks if the Editor is playing or Profiler is recording
      if (ProfilerDriver.enabled && EditorApplication.isPlaying)
      {
        if(!mySequentialLifetimes.IsCurrentTerminated)
          mySequentialLifetimes.TerminateCurrent();
        return;
      }

      // Get the current profiler window object once to avoid multiple property access
      var profilerWindowObject = myProfilerWindowAdapter?.ProfilerWindowObject as EditorWindow;

      // If the profiler window has changed - create adapter for a new window
      if (profilerWindowObject != lastKnownProfilerWindow)
      {
        ourLogger.Verbose(
          $"Update {nameof(myProfilerWindowAdapter)} because of {nameof(profilerWindowObject)} change: {lastKnownProfilerWindow}");
        myProfilerWindowAdapter = myAdaptersFactory.CreateProfilerWindowAdapter(lastKnownProfilerWindow);

        // Update the profiler window object after creating a new adapter
        profilerWindowObject = myProfilerWindowAdapter?.ProfilerWindowObject as EditorWindow;
      }

      // If the profiler window is closed or destroyed - clear existing cached snapshot information
      if (profilerWindowObject == null || myProfilerWindowAdapter == null)
      {
        if(!mySequentialLifetimes.IsCurrentTerminated)
          mySequentialLifetimes.TerminateCurrent();
        myLastSnapshot.Set(null);
        myProfilerStatus.Set(myLastSnapshot.Value.ToSnapshotStatus(SnapshotStatus.HasNewSnapshotDataToFetch));
        return;
      }

      UpdateSnapshotStatus(myProfilerWindowAdapter);
    }

    private Task<UnityProfilerSnapshot?> FetchNewSnapshotData(Lifetime lifetime,
      ProfilerSnapshotRequest snapshotRequest)
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

    private void UpdateSnapshotStatus(IProfilerWindowAdapter profilerWindowAdapter)
    {
      // Early return if adapter is null
      if (myProfilerWindowAdapter == null)
        return;

      // Get the selected frame index once
      var selectedFrameIndex = profilerWindowAdapter.GetSelectedFrameIndex();

      // Early return if no frame is selected
      if (selectedFrameIndex == -1)
        return;

      // Cache the current status value to avoid multiple property access
      var currentStatus = myProfilerStatus.Value;

      // Skip update if the status is already up-to-date for this frame (unless it's NoSnapshotDataAvailable)
      if (currentStatus != null &&
          currentStatus.FrameIndex == selectedFrameIndex &&
          currentStatus.Status != SnapshotStatus.NoSnapshotDataAvailable)
        return;

      // Get and set the new status
      var newStatusInfo = mySnapshotCrawler.GetCurrentProfilerSnapshotStatusInfo(selectedFrameIndex, 0);
      myProfilerStatus.Set(newStatusInfo);
    }

    private Task<UnityProfilerSnapshot?> StartSnapshotFetchingTask(ProfilerSnapshotRequest snapshotRequest,
      Lifetime lifetime)
    {
      ourLogger.Verbose("StartSnapshotFetchingTask");

      // Create a lifetime that will be terminated when a new task starts or the parent lifetime ends
      var snapshotFetchingLifetime = lifetime.Intersect(mySequentialLifetimes.Next());

      // Create progress reporter once and reuse it
      var progress = new Progress<UnityProfilerSnapshotStatus>(snapshotStatus =>
        snapshotFetchingLifetime.Execute(() => myProfilerStatus.Set(snapshotStatus)));

      // Start a new task
      return Task.Run(async () =>
      {
        try
        {
          // Get the snapshot data
          var profilerFrameSnapshot =
            await mySnapshotCrawler.GetUnityProfilerSnapshot(snapshotRequest, snapshotFetchingLifetime, progress);

          // Update the last snapshot if the lifetime is still alive
          if (snapshotFetchingLifetime.IsAlive)
            myLastSnapshot.Set(profilerFrameSnapshot);

          return profilerFrameSnapshot;
        }
        catch (OperationCanceledException)
        {
          ourLogger.Verbose($"Task {nameof(mySnapshotCrawler.GetUnityProfilerSnapshot)} was canceled");

          // Only set to null if the lifetime is still alive
          if (snapshotFetchingLifetime.IsAlive)
            myLastSnapshot.Set(null);

          return null;
        }
        catch (Exception ex)
        {
          ourLogger.Error($"Task {nameof(mySnapshotCrawler.GetUnityProfilerSnapshot)} failed with exception", ex);

          // Only set to null if the lifetime is still alive
          if (snapshotFetchingLifetime.IsAlive)
            myLastSnapshot.Set(null);

          return null;
        }
      }, snapshotFetchingLifetime);
    }
  }
}
