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

[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public class UnityProfilerSnapshotProvider : IUnityProfilerSnapshotDataProvider
{
    private static readonly UnityProfilerSnapshotStatus ourUnityProfilerSnapshotStatusDisabled =
        new(-1, -1, string.Empty, -1, SnapshotStatus.Disabled, 0);

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
    private readonly SettingsScalarEntry mySnapshotFetchingScalarEntry;
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

        mySnapshotFetchingScalarEntry = mySettingsStore.Schema.GetScalarEntry(static (UnitySettings s) => s.ProfilerSnapshotFetchingSettings);
        mySnapshotGutterMarksDisplaySettingsScalarEntry = mySettingsStore.Schema.GetScalarEntry(static (UnitySettings s) => s.ProfilerGutterMarksDisplaySettings);

        // Set up all advice and subscriptions
        AdviseOnSettingsChanges();
        AdviseOnFrontendRequests();
        AdviseOnUnityProfilerSnapshotStatus();
    }

    private void AdviseOnSettingsChanges()
    {
        myFrontendBackendHost.Model.NotNull().UnityApplicationSettings.ProfilerSnapshotFetchingSettings.Advise(myLifetime,
            settings =>
            {
                try
                {
                    var profilerSnapshotFetchingSettings = settings.ToEnum<ProfilerSnapshotFetchingSettings>();
                    mySettingsStore.SetValue(mySnapshotFetchingScalarEntry, profilerSnapshotFetchingSettings, null);
                }
                catch (Exception e)
                {
                    myLogger.LogException(e);
                }
            });
        
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

        await InvalidateDaemon(lifetime);
    }
    
    private async Task HandleSnapshotFetchingSettingsChange(SettingsStoreChangeArgs change, Lifetime lt)
    {
        if (!change.ChangedEntries.Contains(mySnapshotFetchingScalarEntry))
            return;

        var fetchingSettings = GetSnapshotFetchingSettings();

        var snapshotStatus = myBackendUnityHost.BackendUnityProfilerModel.Value?.ProfilerSnapshotStatus.Value;
            
        switch (fetchingSettings)
        {
            case ProfilerSnapshotFetchingSettings.AutoFetch:
            case ProfilerSnapshotFetchingSettings.ManualFetch:
            {
                myLogger.Verbose( $"Start snapshot auto fetching after settings changed to {fetchingSettings}");
                    
                if (snapshotStatus == null)
                    return;

                FrontendBackendProfilerModel.ProfilerSnapshotStatus.Value = snapshotStatus;
                    
                var snapshotRequest = new ProfilerSnapshotRequest(snapshotStatus.FrameIndex, snapshotStatus.ThreadIndex);
                FetchProfilerSnapshotWithProgress(snapshotRequest);
                return;
            }
            case ProfilerSnapshotFetchingSettings.Disabled:
                myLogger.Verbose( $"Stop snapshot auto fetching after settings changed to {fetchingSettings}");
                FrontendBackendProfilerModel.ProfilerSnapshotStatus.Value = ourUnityProfilerSnapshotStatusDisabled;
                myRequestSnapshotSeqLifetimes.TerminateCurrent();
                UpdateSampleCache(null);
                await InvalidateDaemon(lt);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(fetchingSettings));
        }
    }

    private void AdviseOnFrontendRequests()
    {
        FrontendBackendProfilerModel.UpdateUnityProfilerSnapshotData
            .Advise(myLifetime, request =>
            {
                if (GetSnapshotFetchingSettings() == ProfilerSnapshotFetchingSettings.Disabled)
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
            catch (Exception e)
            {
                myLogger.LogException(e);
            }
        });
        FrontendBackendProfilerModel.SetGutterMarksRenderSetting.Advise(myLifetime, void (renderSetting) =>
        {
            try
            {
                var highlightingSettings = renderSetting.ToProfilerSnapshotHighlightingSettings();
                mySettingsStore.SetValue(mySnapshotGutterMarksDisplaySettingsScalarEntry, highlightingSettings, null);
            }
            catch (Exception e)
            {
                myLogger.LogException(e);
            }
        });
        
        FrontendBackendProfilerModel.ShowPopupAction.Advise(myLifetime, () => myUnityProfilerInfoCollector.OnNavigationPopupShown());
    }

    private void AdviseOnUnityProfilerSnapshotStatus()
    {
        myBackendUnityHost.BackendUnityProfilerModel!.ViewNotNull<UnityProfilerModel>(
            myLifetime, (lt, unityProfilerModel) =>
                unityProfilerModel.ProfilerSnapshotStatus.AdviseNotNull(lt, async void (status) =>
                {
                    try
                    {
                        await HandleSnapshotStatusChange(status, lt);
                    }
                    catch (Exception e)
                    {
                        myLogger.LogException(e);
                    }
                }));
    }

    private void FetchProfilerSnapshotWithProgress(ProfilerSnapshotRequest snapshotRequest)
    {
        var requestLifetime = myRequestSnapshotSeqLifetimes.Next();
        var fetchingProgressLifetimeDefinition = new SequentialLifetimes(requestLifetime);
        StartFetchingProgressTask(fetchingProgressLifetimeDefinition.Next());
        requestLifetime.StartBackgroundAsync(async () =>
        {
            myLogger.Verbose($"Requesting snapshot {snapshotRequest}");
            try
            {
                var getUnityProfilerSnapshotCall = myBackendUnityHost.BackendUnityProfilerModel.Maybe.ValueOrDefault?
                    .GetUnityProfilerSnapshot;

                if (getUnityProfilerSnapshotCall == null)
                    return;

                var currentProfilerFrameSnapshot =
                    await getUnityProfilerSnapshotCall.Start(requestLifetime, snapshotRequest);
                var samplesCount = currentProfilerFrameSnapshot?.Samples.Count ?? -1;
                myLogger.Verbose($"Succesfully recived snapthot information, samples count {samplesCount}");

                try
                {
                    var cacheUpdatingProgressProperty = new Property<double>("SnapshotCacheCalculation::Progress", 0.0).EnsureNotOutside(0.0, 1.0);
                    var cacheUpdatingProgress = new Progress<double>(value => cacheUpdatingProgressProperty.Value = value);
                    StartCacheUpdateProgressTask(fetchingProgressLifetimeDefinition.Next(), cacheUpdatingProgressProperty);
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
            catch (Exception e)
            {
                myLogger.LogException(e);
            }
            finally
            {
                fetchingProgressLifetimeDefinition.TerminateCurrent();
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

    private async Task HandleSnapshotStatusChange(UnityProfilerSnapshotStatus snapshotStatus, Lifetime lifetime)
    {
        myLogger.Verbose($"Updated snapshot status: {snapshotStatus}");

        if(snapshotStatus.Status == SnapshotStatus.HasNewSnapshotDataToFetch)
            myUnityProfilerInfoCollector.OnUnityProfilerFrameSelected(snapshotStatus.SamplesCount);
        
        if (GetSnapshotFetchingSettings() == ProfilerSnapshotFetchingSettings.Disabled)
        {
            FrontendBackendProfilerModel.ProfilerSnapshotStatus.Set(ourUnityProfilerSnapshotStatusDisabled);
            // Ensure any ongoing snapshot request and related progress tasks are terminated to avoid leaks
            myRequestSnapshotSeqLifetimes.TerminateCurrent();
            return;
        }

        FrontendBackendProfilerModel.ProfilerSnapshotStatus.Set(snapshotStatus);

        UpdateProgressbarDescription(snapshotStatus);
        
        
        if(snapshotStatus.Status == SnapshotStatus.NoSnapshotDataAvailable)
        {
            UpdateSampleCache(null);
            await InvalidateDaemon(lifetime);
        }

        if (snapshotStatus.Status != SnapshotStatus.HasNewSnapshotDataToFetch)
            return;
        if (snapshotStatus.Equals(myPooledSamplesCache?.SnapshotInfo))
            return;
        if (GetSnapshotFetchingSettings() != ProfilerSnapshotFetchingSettings.AutoFetch)
            return;

        myLogger.Verbose($"Auto fetching snapshot {snapshotStatus}");
        var snapshotRequest = new ProfilerSnapshotRequest(snapshotStatus.FrameIndex, snapshotStatus.ThreadIndex);
        FetchProfilerSnapshotWithProgress(snapshotRequest);
    }

    private void UpdateProgressbarDescription(UnityProfilerSnapshotStatus snapshotStatus)
    {
        myProgress.Value = snapshotStatus.FetchingProgress;
        myFetchingProgressProperty.Value =
            string.Format(Strings.UnityProfilerSnapshot_Fetching_Profiler_snapshot_description,
                snapshotStatus.FrameIndex, snapshotStatus.ThreadName);
    }

    private ProfilerSnapshotFetchingSettings GetSnapshotFetchingSettings()
    {
        return mySettingsStore.GetValue(mySnapshotFetchingScalarEntry, null) is ProfilerSnapshotFetchingSettings
            fetchingSettings
            ? fetchingSettings
            : ProfilerSnapshotFetchingSettings.Disabled;
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
    }
}