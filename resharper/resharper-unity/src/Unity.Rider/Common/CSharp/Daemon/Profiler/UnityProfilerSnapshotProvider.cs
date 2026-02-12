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
        });
    }

    public ProfilerSnapshotHighlightingSettings GetGutterMarkSettings()
    {
        return mySettingsStore.GetValue(mySnapshotGutterMarksDisplaySettingsScalarEntry, null) is
            ProfilerSnapshotHighlightingSettings
            fetchingSettings
            ? fetchingSettings
            : ProfilerSnapshotHighlightingSettings.Default;
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
        
        frontendBackendModel?.IsIntegraionEnable.Value = snapshotFetchingEnabled;
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
        FrontendBackendProfilerModel.IsIntegraionEnable.Value = GetSnapshotFetchingEnabled();

        FrontendBackendProfilerModel.IsIntegraionEnable.Advise(myLifetime,
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
        var requestLifetime = myRequestSnapshotSeqLifetimes.Next();
        StartFetchingProgressTask(requestLifetime);
        requestLifetime.StartBackgroundAsync(async () =>
        {
            myLogger.Verbose($"Requesting snapshot {snapshotRequest}");
            try
            {
                var requestFrameSnapshotCall = myBackendUnityHost.BackendUnityProfilerModel.Maybe.ValueOrDefault?
                    .RequestFrameSnapshot;

                if (requestFrameSnapshotCall == null)
                    return;

                var snapshotRequestTask = await requestFrameSnapshotCall.Start(requestLifetime, snapshotRequest);
                
                if (snapshotRequestTask == null)
                {
                    myLogger.Warn("RequestFrameSnapshot returned null");
                    return;
                }

                // Observe progress updates
                snapshotRequestTask.Progress.Advise(requestLifetime, progress =>
                {
                    myProgress.Value = progress;
                    myLogger.Trace($"Snapshot fetching progress: {progress:P1}");
                });

                // Observe status updates
                snapshotRequestTask.Status.Advise(requestLifetime, status =>
                {
                    myLogger.Verbose($"Snapshot request status: {status}");
                });

                // Wait for snapshot to be available using TaskCompletionSource
                var snapshotTcs = new TaskCompletionSource<UnityProfilerSnapshot?>();
                requestLifetime.Bracket(
                    () => snapshotRequestTask.Snapshot.Advise(requestLifetime, snapshot =>
                    {
                        if (snapshot != null)
                            snapshotTcs.TrySetResult(snapshot);
                    }),
                    () => snapshotTcs.TrySetCanceled()
                );
                
                var currentProfilerFrameSnapshot = await snapshotTcs.Task;
                
                var samplesCount = currentProfilerFrameSnapshot?.Samples.Count ?? -1;
                myLogger.Verbose($"Successfully received snapshot information, samples count {samplesCount}");

                try
                {
                    var cacheUpdatingProgressProperty = new Property<double>("SnapshotCacheCalculation::Progress", 0.0).EnsureNotOutside(0.0, 1.0);
                    var cacheUpdatingProgress = new Progress<double>(value => cacheUpdatingProgressProperty.Value = value);
                    StartCacheUpdateProgressTask(requestLifetime, cacheUpdatingProgressProperty);
                    UpdateSampleCache(currentProfilerFrameSnapshot, cacheUpdatingProgress);
                    if (samplesCount > 0)
                        myUnityProfilerInfoCollector.OnSnapshotFetched(samplesCount);
                }
                catch (Exception e)
                {
                    myLogger.LogException(e);
                }

                await InvalidateDaemon(requestLifetime);
                  
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                myLogger.LogException(e);
            }
            finally
            {
                myRequestSnapshotSeqLifetimes.TerminateCurrent();
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