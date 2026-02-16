#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.Collections.Viewable;
using JetBrains.DataFlow;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.ProjectsHost.SolutionHost.Progress;
using JetBrains.Rd.Base;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Rider.Resources;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model.Unity;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Rider.Model.Unity.FrontendBackend;
using JetBrains.Threading;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.Profiler;

[DerivedComponentsInstantiationRequirement(InstantiationRequirement.DeadlockSafe)]
public interface IUnityProfilerSnapshotDataProvider
{
    public bool TryGetSamplesByQualifiedName(string qualifiedName, ref IList<PooledSample> samples);
    public bool TryGetTypeSamples(string qualifiedName, ref IList<PooledSample> samples);
    public ProfilerSnapshotHighlightingSettings GetGutterMarkSettings();
    public bool IsGutterMarksEnabled();
}

[SolutionComponent(Instantiation.ContainerAsyncAnyThreadSafe)]
public class UnityProfilerSnapshotProvider : IUnityProfilerSnapshotDataProvider
{
    private readonly Lifetime myLifetime;
    private readonly SequentialLifetimes myRequestSnapshotSeqLifetimes;

    private readonly BackendUnityHost myBackendUnityHost;
    private readonly FrontendBackendHost myFrontendBackendHost;
    private readonly ILogger myLogger;
    private readonly ISolution mySolution;
    private PooledSamplesCache? myPooledSamplesCache;

    private readonly Property<double> myProgress = new Property<double>("SnapshotFetching::Progress", 0.0).EnsureNotOutside(0.0, 1.0);

    private readonly IProperty<string> myFetchingProgressProperty = new Property<string>("SnapshotFetching::Description");
    private readonly IContextBoundSettingsStoreLive mySettingsStore;
    private readonly SettingsScalarEntry myEnableSnapshotFetchingScalarEntry;
    private readonly SettingsScalarEntry mySnapshotFetchingModeScalarEntry;
    private readonly SettingsScalarEntry myIsGutterMarksEnabledEntry;
    private readonly SettingsScalarEntry mySnapshotGutterMarksDisplaySettingsScalarEntry;

    private readonly BackgroundProgressManager myBackgroundProgressManager;
    private readonly UnityProfilerInfoCollector myUnityProfilerInfoCollector;
    private FrontendBackendProfilerModel FrontendBackendProfilerModel => myFrontendBackendHost.Model.GetFrontendBackendProfilerModel().NotNull();
    
    public UnityProfilerSnapshotProvider(Lifetime lifetime,
        ISolution solution,
        BackendUnityHost backendUnityHost,
        FrontendBackendHost frontendBackendHost,
        ISettingsStore settingsStore,
        BackgroundProgressManager backgroundProgressManager,
        UnityProfilerInfoCollector unityProfilerInfoCollector,
        ILogger logger)
    {
        myLifetime = lifetime;
        myRequestSnapshotSeqLifetimes = new SequentialLifetimes(myLifetime);
        myBackendUnityHost = backendUnityHost;
        myFrontendBackendHost = frontendBackendHost;
        myLogger = logger;
        myBackgroundProgressManager = backgroundProgressManager;
        myUnityProfilerInfoCollector = unityProfilerInfoCollector;
        mySolution = solution;
        mySettingsStore = settingsStore.BindToContextLive(myLifetime, ContextRange.ApplicationWide);

        myEnableSnapshotFetchingScalarEntry = mySettingsStore.Schema.GetScalarEntry(static (UnitySettings s) => s.EnableProfilerSnapshotFetching);
        mySnapshotFetchingModeScalarEntry = mySettingsStore.Schema.GetScalarEntry(static (UnitySettings s) => s.ProfilerSnapshotFetchingMode);
        myIsGutterMarksEnabledEntry = mySettingsStore.Schema.GetScalarEntry(static (UnitySettings s) => s.IsProfilerGutterMarksDisplayEnabled);
        mySnapshotGutterMarksDisplaySettingsScalarEntry = mySettingsStore.Schema.GetScalarEntry(static (UnitySettings s) => s.ProfilerGutterMarksDisplaySettings);

        // Set up all advice and subscriptions
        AdviseOnSettingsChanges();
        AdviseOnFrontendRequests();
        AdviseOnUnityProfilerModel();
    }

    private void AdviseOnSettingsChanges()
    {
        mySettingsStore.AdviseAsyncChanged(myLifetime, async (lt, change) =>
        {
            await HandleSnapshotFetchingSettingsChange(change, lt);
            await HandleGutterMarkDisplaySettingsChange(change, lt);
            await HandleGutterEnabledSettingsChange(change, lt);
        });
    }

    public bool IsGutterMarksEnabled()
    {
        return mySettingsStore.GetValue(myIsGutterMarksEnabledEntry, null) is true;
    }
    
    public ProfilerSnapshotHighlightingSettings GetGutterMarkSettings()
    {
        return mySettingsStore.GetValue(mySnapshotGutterMarksDisplaySettingsScalarEntry, null) is
            ProfilerSnapshotHighlightingSettings
            fetchingSettings
            ? fetchingSettings
            : ProfilerSnapshotHighlightingSettings.Default;
    }

    private async Task HandleGutterEnabledSettingsChange(SettingsStoreChangeArgs change, Lifetime lifetime)
    {
        if (!change.ChangedEntries.Contains(myIsGutterMarksEnabledEntry))
            return;

        FrontendBackendProfilerModel.IsGutterMarksEnabled.Value = IsGutterMarksEnabled();
        await InvalidateDaemon(lifetime);
    }
    
    private async Task HandleGutterMarkDisplaySettingsChange(SettingsStoreChangeArgs change, Lifetime lifetime)
    {
        if (!change.ChangedEntries.Contains(mySnapshotGutterMarksDisplaySettingsScalarEntry))
            return;

        FrontendBackendProfilerModel.GutterMarksRenderSettings.Value =
            GetGutterMarkSettings().ToProfilerGutterMarkRenderSettings();
        await InvalidateDaemon(lifetime);
    }
    
    private async Task HandleSnapshotFetchingSettingsChange(SettingsStoreChangeArgs change, Lifetime lt)
    {
        if (!change.ChangedEntries.Contains(myEnableSnapshotFetchingScalarEntry) &&
            !change.ChangedEntries.Contains(mySnapshotFetchingModeScalarEntry))
            return;

        var frontendBackendModel = myFrontendBackendHost.Model.GetFrontendBackendProfilerModel();

        var snapshotFetchingEnabled = GetSnapshotFetchingEnabled();
        var fetchingMode = GetSnapshotFetchingMode();
        
        frontendBackendModel?.IsIntegrationEnable.Value = snapshotFetchingEnabled;
        frontendBackendModel?.FetchingMode.Value = fetchingMode == ProfilerSnapshotFetchingMode.Auto ? FetchingMode.Auto : FetchingMode.Manual;
        
        if (snapshotFetchingEnabled)
        {
            myLogger.Verbose($"Snapshot fetching enabled, mode: {fetchingMode}");
        }
        else
        {
            myLogger.Verbose("Stop snapshot fetching after settings changed to Disabled");
            myRequestSnapshotSeqLifetimes.TerminateCurrent();
            UpdateSampleCache(null);
            await InvalidateDaemon(lt);
        }
    }

    private void AdviseOnFrontendRequests()
    {
        FrontendBackendProfilerModel.UpdateUnityProfilerSnapshotData
            .Advise(myLifetime, request =>
            {
                if (!GetSnapshotFetchingEnabled())
                    return;

                myLogger.Verbose($"Requesting snapshot requested by frontend: {request}");
                FetchProfilerSnapshotWithProgress(request);
            });
        FrontendBackendProfilerModel.NavigateByQualifiedName.Advise(myLifetime, void (qualifiedName) =>
        {
            try
            {
                myLifetime.StartReadAndMainThreadActionAsync(locks =>
                {
                    return locks.MainReadAction(() =>
                    {
                        myUnityProfilerInfoCollector.OnNavigateToParentCall();
                        ProfilerNavigationUtils.ParseAndNavigateToParent(mySolution, qualifiedName, myLogger);
                    });
                }).NoAwait();
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                myLogger.LogException(e);
            }
        });
        
        //settings
        FrontendBackendProfilerModel.IsIntegrationEnable.Value = GetSnapshotFetchingEnabled();

        FrontendBackendProfilerModel.IsIntegrationEnable.Advise(myLifetime,
            enabled => { mySettingsStore.SetValue(myEnableSnapshotFetchingScalarEntry, enabled, null); });
        
        FrontendBackendProfilerModel.FetchingMode.Value = GetSnapshotFetchingMode() == ProfilerSnapshotFetchingMode.Auto ? FetchingMode.Auto : FetchingMode.Manual;
        FrontendBackendProfilerModel.FetchingMode.Advise(myLifetime, mode => {
                var fetchingMode = mode switch
                {
                    FetchingMode.Auto => ProfilerSnapshotFetchingMode.Auto,
                    FetchingMode.Manual => ProfilerSnapshotFetchingMode.Manual,
                    _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
                };
                
                mySettingsStore.SetValue(mySnapshotFetchingModeScalarEntry, fetchingMode, null);
            });

        FrontendBackendProfilerModel.IsGutterMarksEnabled.Value = IsGutterMarksEnabled();
        FrontendBackendProfilerModel.IsGutterMarksEnabled.Advise(myLifetime,
            enabled => { mySettingsStore.SetValue(myIsGutterMarksEnabledEntry, enabled, null); });
        
        FrontendBackendProfilerModel.GutterMarksRenderSettings.Value =
            GetGutterMarkSettings().ToProfilerGutterMarkRenderSettings();
        FrontendBackendProfilerModel.GutterMarksRenderSettings.Advise(myLifetime, void (renderSetting) =>
        {
            try
            {
                var highlightingSettings = renderSetting.ToProfilerSnapshotHighlightingSettings();
                mySettingsStore.SetValue(mySnapshotGutterMarksDisplaySettingsScalarEntry, highlightingSettings, null);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                myLogger.LogException(e);
            }
        });
    }

    private void AdviseOnUnityProfilerModel()
    {
        myBackendUnityHost.BackendUnityProfilerModel!.ViewNotNull<UnityProfilerModel>(
            myLifetime, (lt, unityProfilerModel) =>
            {
                unityProfilerModel.MainThreadTimingsAndThreads.FlowIntoRdSafe(lt, FrontendBackendProfilerModel.MainThreadTimingsAndThreads);
                unityProfilerModel.CurrentProfilerRecordInfo.FlowIntoRdSafe(lt, FrontendBackendProfilerModel.CurrentProfilerRecordInfo);
                
                unityProfilerModel.SelectionState.FlowIntoRdSafe(lt, FrontendBackendProfilerModel.SelectionState);
                FrontendBackendProfilerModel.SelectionState.FlowIntoRdSafe(lt, unityProfilerModel.SelectionState);
                unityProfilerModel.SelectionState.Advise(lt, state =>
                {
                    FrontendBackendProfilerModel.SelectionState.Set(state);
                });
                FrontendBackendProfilerModel.SelectionState.Advise(lt, 
                    state=>
                    {
                        unityProfilerModel.SelectionState.Set(state);
                    });
            });
    }

    private void FetchProfilerSnapshotWithProgress(ProfilerSnapshotRequest snapshotRequest)
    {
        myLogger.Info($"FetchProfilerSnapshotWithProgress: Starting snapshot fetch for request: {snapshotRequest}");
        
        var requestLifetime = myRequestSnapshotSeqLifetimes.Next();
        myLogger.Verbose("FetchProfilerSnapshotWithProgress: Created new request lifetime");
        
        StartFetchingProgressTask(requestLifetime);
        myLogger.Verbose("FetchProfilerSnapshotWithProgress: Started progress task");
        
        requestLifetime.StartBackgroundAsync(async () =>
        {
            myLogger.Info($"FetchProfilerSnapshotWithProgress: Background task started for snapshot {snapshotRequest}");
            try
            {
                myLogger.Verbose("FetchProfilerSnapshotWithProgress: Retrieving BackendUnityProfilerModel");
                var requestFrameSnapshotCall = myBackendUnityHost.BackendUnityProfilerModel.Maybe.ValueOrDefault?
                    .RequestFrameSnapshot;

                if (requestFrameSnapshotCall == null)
                {
                    myLogger.Warn("FetchProfilerSnapshotWithProgress: RequestFrameSnapshot is null, aborting");
                    return;
                }

                myLogger.Verbose("FetchProfilerSnapshotWithProgress: Calling RequestFrameSnapshot.Start");
                var snapshotRequestTask = await requestFrameSnapshotCall.Start(requestLifetime, snapshotRequest);
                
                if (snapshotRequestTask == null)
                {
                    myLogger.Warn("FetchProfilerSnapshotWithProgress: RequestFrameSnapshot returned null task");
                    return;
                }

                myLogger.Verbose("FetchProfilerSnapshotWithProgress: Successfully started snapshot request task");

                // Wait for snapshot to be available using TaskCompletionSource
                myLogger.Verbose("FetchProfilerSnapshotWithProgress: Setting up snapshot completion listener");
                var snapshotTcs = new TaskCompletionSource<UnityProfilerSnapshot?>();
                
                // Check initial status - if already completed/failed, we might have missed the update
                var initialStatus = snapshotRequestTask.Status.Value;
                myLogger.Verbose($"FetchProfilerSnapshotWithProgress: Initial task status: {initialStatus}");
                
                // Check if snapshot is already available (race condition: snapshot completed before we set up advisors)
                if (snapshotRequestTask.Snapshot.HasValue())
                {
                    var existingSnapshot = snapshotRequestTask.Snapshot.Value;
                    var count = existingSnapshot?.Samples.Count ?? 0;
                    myLogger.Info($"FetchProfilerSnapshotWithProgress: Snapshot already available with {count} samples");
                    snapshotTcs.TrySetResult(existingSnapshot);
                }
                else if (initialStatus != JetBrains.Rider.Model.Unity.BackendUnity.TaskStatus.Running)
                {
                    // Task already completed but no snapshot was set
                    myLogger.Warn($"FetchProfilerSnapshotWithProgress: Task status is {initialStatus} but no snapshot available");
                    if (initialStatus == JetBrains.Rider.Model.Unity.BackendUnity.TaskStatus.Failed)
                    {
                        myLogger.Error($"FetchProfilerSnapshotWithProgress: Task failed: {snapshotRequestTask.ErrorMessage.Value}");
                        snapshotTcs.TrySetResult(null);
                    }
                    else if (initialStatus == JetBrains.Rider.Model.Unity.BackendUnity.TaskStatus.Cancelled)
                    {
                        myLogger.Info("FetchProfilerSnapshotWithProgress: Task was cancelled");
                        snapshotTcs.TrySetCanceled();
                    }
                    else
                    {
                        // Completed without snapshot - treat as null result
                        myLogger.Warn("FetchProfilerSnapshotWithProgress: Task completed but snapshot not set, treating as null");
                        snapshotTcs.TrySetResult(null);
                    }
                }
                else
                {
                    myLogger.Verbose("FetchProfilerSnapshotWithProgress: Setting up property observers");
                    
                    // Observe progress updates
                    snapshotRequestTask.Progress.Advise(requestLifetime, progress =>
                    {
                        myProgress.Value = progress;
                        myLogger.Trace($"FetchProfilerSnapshotWithProgress: Progress update: {progress:P1}");
                    });

                    // Set up snapshot observer
                    requestLifetime.Bracket(
                        () =>
                        {
                            myLogger.Verbose("FetchProfilerSnapshotWithProgress: Registering Snapshot.Advise");
                            snapshotRequestTask.Snapshot.Advise(requestLifetime, snapshot =>
                            {
                                var count = snapshot?.Samples.Count ?? 0;
                                myLogger.Info($"FetchProfilerSnapshotWithProgress: !!! Snapshot.Advise callback fired with {count} samples");
                                
                                // Complete the task regardless of whether snapshot is null or not
                                if (snapshot != null)
                                {
                                    myLogger.Info($"FetchProfilerSnapshotWithProgress: Snapshot received successfully, completing task");
                                    var setResult = snapshotTcs.TrySetResult(snapshot);
                                    myLogger.Verbose($"FetchProfilerSnapshotWithProgress: TrySetResult returned {setResult}");
                                }
                                else
                                {
                                    myLogger.Info($"FetchProfilerSnapshotWithProgress: Snapshot received with null value, completing task with null");
                                    if(snapshotRequestTask.ErrorMessage.HasValue())
                                        myLogger.Error($"FetchProfilerSnapshotWithProgress: Snapshot request failed with error: {snapshotRequestTask.ErrorMessage.Value}");
                                    
                                    var setResult = snapshotTcs.TrySetResult(null);
                                    myLogger.Verbose($"FetchProfilerSnapshotWithProgress: TrySetResult(null) returned {setResult}");
                                }
                            });
                        },
                        () =>
                        {
                            myLogger.Verbose("FetchProfilerSnapshotWithProgress: Request lifetime terminated, canceling snapshot task");
                            snapshotTcs.TrySetCanceled();
                        }
                    );
                    
                    // Also observe Status to complete the task if it fails or is cancelled
                    myLogger.Verbose("FetchProfilerSnapshotWithProgress: Registering Status.Advise");
                    snapshotRequestTask.Status.Advise(requestLifetime, status =>
                    {
                        myLogger.Info($"FetchProfilerSnapshotWithProgress: !!! Status.Advise callback fired: {status}, TCS completed: {snapshotTcs.Task.IsCompleted}");
                        
                        // If task failed/cancelled but snapshot wasn't set yet, complete the TCS
                        if (!snapshotTcs.Task.IsCompleted)
                        {
                            if (status == JetBrains.Rider.Model.Unity.BackendUnity.TaskStatus.Failed)
                            {
                                myLogger.Warn($"FetchProfilerSnapshotWithProgress: Task failed: {snapshotRequestTask.ErrorMessage.Value}");
                                snapshotTcs.TrySetResult(null);
                            }
                            else if (status == JetBrains.Rider.Model.Unity.BackendUnity.TaskStatus.Cancelled)
                            {
                                myLogger.Info("FetchProfilerSnapshotWithProgress: Task cancelled");
                                snapshotTcs.TrySetCanceled();
                            }
                            else if (status == JetBrains.Rider.Model.Unity.BackendUnity.TaskStatus.Completed)
                            {
                                myLogger.Warn("FetchProfilerSnapshotWithProgress: Status changed to Completed but snapshot not yet received");
                            }
                        }
                    });
                    
                    myLogger.Verbose("FetchProfilerSnapshotWithProgress: All observers registered");
                    snapshotRequestTask.StartSignal.Fire();
                }
                
                myLogger.Verbose("FetchProfilerSnapshotWithProgress: Awaiting snapshot data");
                var currentProfilerFrameSnapshot = await snapshotTcs.Task;
                
                var samplesCount = currentProfilerFrameSnapshot?.Samples.Count ?? -1;
                myLogger.Info($"FetchProfilerSnapshotWithProgress: Successfully received snapshot, samples count: {samplesCount}");

                try
                {
                    myLogger.Verbose("FetchProfilerSnapshotWithProgress: Starting cache update");
                    var cacheUpdatingProgressProperty = new Property<double>("SnapshotCacheCalculation::Progress", 0.0).EnsureNotOutside(0.0, 1.0);
                    var cacheUpdatingProgress = new Progress<double>(value => cacheUpdatingProgressProperty.Value = value);
                    StartCacheUpdateProgressTask(requestLifetime, cacheUpdatingProgressProperty);
                    UpdateSampleCache(currentProfilerFrameSnapshot, cacheUpdatingProgress);
                    myLogger.Verbose("FetchProfilerSnapshotWithProgress: Cache update completed");
                    
                    if (samplesCount > 0)
                    {
                        myLogger.Verbose($"FetchProfilerSnapshotWithProgress: Notifying profiler info collector about {samplesCount} samples");
                        myUnityProfilerInfoCollector.OnSnapshotFetched(samplesCount);
                    }
                }
                catch (Exception e)
                {
                    myLogger.Error("FetchProfilerSnapshotWithProgress: Error during cache update", e);
                    myLogger.LogException(e);
                }

                myLogger.Verbose("FetchProfilerSnapshotWithProgress: Invalidating daemon");
                await InvalidateDaemon(requestLifetime);
                myLogger.Info("FetchProfilerSnapshotWithProgress: Snapshot fetching completed successfully");
                  
            }
            catch (TaskCanceledException e)
            {
                myLogger.Info($"FetchProfilerSnapshotWithProgress: Snapshot fetch was canceled: {e.Message}");
            }
            catch (OperationCanceledException e)
            {
                myLogger.Info($"FetchProfilerSnapshotWithProgress: Operation was canceled: {e.Message}");
            }
            catch (Exception e)
            {
                myLogger.Error("FetchProfilerSnapshotWithProgress: Unexpected error during snapshot fetch", e);
                myLogger.LogException(e);
            }
            finally
            {
                myLogger.Verbose("FetchProfilerSnapshotWithProgress: Terminating request lifetime");
                myRequestSnapshotSeqLifetimes.TerminateCurrent();
                myLogger.Verbose("FetchProfilerSnapshotWithProgress: Cleanup completed");
            }
        }).NoAwait();
    }

    private async Task InvalidateDaemon(Lifetime lifetime)
    {
        myLogger.Verbose("Invalidating daemon");
        await lifetime.StartMainRead(() => DaemonBase.GetInstance(mySolution).Invalidate()); 
    }

    private void StartCacheUpdateProgressTask(Lifetime lifetime, Property<double> cacheUpdatingProgressProperty)
    {
        lifetime.StartMainRead(() =>
            myBackgroundProgressManager.AddNewTask(lifetime,
                BackgroundProgressBuilder.Create()
                    .WithHeader(Strings.UnityProfilerSnapshot_Update_Profiler_Snapshot_Cache_header)
                    .WithProgress(cacheUpdatingProgressProperty)
                    .AsNonCancelable()
            )
        );
    }

    private void StartFetchingProgressTask(Lifetime lifetime)
    {
        lifetime.StartMainRead(() =>
            myBackgroundProgressManager.AddNewTask(lifetime,
                BackgroundProgressBuilder.Create()
                    .WithHeader(Strings.UnityProfilerSnapshot_Fetching_Profiler_snapshot_header)
                    .WithDescription(myFetchingProgressProperty)
                    .WithProgress(myProgress)
                    .AsNonCancelable()
            )
        );
    }

    private bool GetSnapshotFetchingEnabled()
    {
        return mySettingsStore.GetValue(myEnableSnapshotFetchingScalarEntry, null) is true;
    }

    private ProfilerSnapshotFetchingMode GetSnapshotFetchingMode()
    {
        return mySettingsStore.GetValue(mySnapshotFetchingModeScalarEntry, null) is ProfilerSnapshotFetchingMode mode
            ? mode
            : ProfilerSnapshotFetchingMode.Auto;
    }

    
    public bool TryGetSamplesByQualifiedName(string qualifiedName, ref IList<PooledSample> samples)
    {
        if (myPooledSamplesCache?.TryGetSamplesByQualifiedName(qualifiedName, ref samples) == true)
            return true;

        myLogger.Trace($"TryGetSamplesByQualifiedName: {qualifiedName} not found");
        return false;
    }

    public bool TryGetTypeSamples(string qualifiedName, ref IList<PooledSample> samples)
    {
        if (myPooledSamplesCache?.TryGetTypeSamples(qualifiedName, ref samples) == true)
            return true;

        myLogger.Trace($"TryGetTypeSamples: {qualifiedName} not found");
        return false;
    }

    private void UpdateSampleCache(UnityProfilerSnapshot? snapshotResult, Progress<double>? cacheUpdatingProgress = null)
    {
        myLogger.Verbose($"Updating sample cache, samples count {snapshotResult?.Samples.Count ?? -1}");
        // ReSharper disable once NotDisposedResource
        var oldCache = Interlocked.Exchange(ref myPooledSamplesCache,
            SamplesCacheUtils.ConstructCache(snapshotResult, cacheUpdatingProgress));
        oldCache?.Dispose();
        FrontendBackendProfilerModel.CurrentSnapshot.Set(myPooledSamplesCache?.GetFrontendModelSnapshot());
    }
}