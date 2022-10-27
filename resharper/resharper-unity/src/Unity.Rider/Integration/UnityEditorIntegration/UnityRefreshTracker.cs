using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Application.changes;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.ProjectsHost.SolutionHost.Progress;
using JetBrains.Rd.Tasks;
using JetBrains.RdBackend.Common.Features;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Rider.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Rider.Backend.Features.FileSystem;
using JetBrains.Rider.Model;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Threading;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.UnityEditorIntegration
{
    [SolutionComponent]
    public class UnityRefresher
    {
        private readonly IShellLocks myLocks;
        private readonly Lifetime myLifetime;
        private readonly ISolution mySolution;
        private readonly BackendUnityHost myBackendUnityHost;
        private readonly ILogger myLogger;
        private readonly UnityVersion myUnityVersion;
        private readonly IContextBoundSettingsStoreLive myBoundSettingsStore;

        public UnityRefresher(IShellLocks locks, Lifetime lifetime, ISolution solution,
                              BackendUnityHost backendUnityHost,
                              IApplicationWideContextBoundSettingStore settingsStore,
                              ILogger logger, UnityVersion unityVersion)
        {
            myLocks = locks;
            myLifetime = lifetime;
            mySolution = solution;
            myBackendUnityHost = backendUnityHost;
            myLogger = logger;
            myUnityVersion = unityVersion;

            if (solution.GetData(ProjectModelExtensions.ProtocolSolutionKey) == null)
                return;

            myBoundSettingsStore = settingsStore.BoundSettingsStore;
        }

        private Task myRunningRefreshTask;

        public void StartRefresh(RefreshType refreshType)
        {
            Refresh(myLifetime, refreshType);
        }

        /// <summary>
        /// Calls Refresh in Unity, and RefreshPaths in vfs. If called multiple times while already running, schedules itself again
        /// </summary>
        /// <param name="lifetime"></param>
        /// <param name="refreshType"></param>
        public Task Refresh(Lifetime lifetime, RefreshType refreshType)
        {
            myLocks.AssertMainThread();

            if (myBackendUnityHost.BackendUnityModel.Value == null)
                return Task.CompletedTask;

            if (!myBoundSettingsStore.GetValue((UnitySettings s) => s.AllowAutomaticRefreshInUnity) &&
                refreshType == RefreshType.Normal)
                return Task.CompletedTask;

            if (myRunningRefreshTask != null && !myRunningRefreshTask.IsCompleted)
            {
                // we may schedule secondary refresh here, which will start after first refresh and protocol reconnect
                // we already do something like that in UnitTesting
                myLogger.Verbose("Refresh already running. Skip starting a new one.");
                return myRunningRefreshTask;
            }

            lifetime.OnTermination(() => myRunningRefreshTask = Task.CompletedTask);

            myRunningRefreshTask = RefreshInternal(lifetime, refreshType);
            return myRunningRefreshTask;
        }

        private async Task RefreshInternal(Lifetime lifetime, RefreshType refreshType)
        {
            myLocks.ReentrancyGuard.AssertGuarded();

            if (myBackendUnityHost.BackendUnityModel.Value == null)
                return;

            if (!myBackendUnityHost.IsConnectionEstablished())
                return;

            var lifetimeDef = Lifetime.Define(lifetime);
            try
            {
                myLogger.Verbose($"myPluginProtocolController.UnityModel.Value.Refresh.StartAsTask, force = {refreshType} Started");
                mySolution.GetComponent<BackgroundProgressManager>().AddNewTask(lifetimeDef.Lifetime,
                    BackgroundProgressBuilder.Create().WithHeader(Strings.UnityRefresher_RefreshInternal_Refreshing_solution_in_Unity_Editor___)
                        .AsIndeterminate().AsNonCancelable());

                var version = myUnityVersion.ActualVersionForSolution.Value;
                try
                {
                    if (version != null && version.Major < 2018)
                    {
                        using (mySolution.GetComponent<VfsListener>().PauseChanges())
                        {
                            try
                            {
                                await myBackendUnityHost.BackendUnityModel.Value.Refresh.Start(lifetimeDef.Lifetime, refreshType).AsTask();
                            }
                            finally
                            {
                                await myLocks.Tasks.YieldTo(myLifetime, Scheduling.MainGuard);
                            }
                        }
                    }
                    else // it is a risk to pause vfs https://github.com/JetBrains/resharper-unity/issues/1601
                        await myBackendUnityHost.BackendUnityModel.Value.Refresh.Start(lifetimeDef.Lifetime, refreshType).AsTask();
                }
                catch (Exception e)
                {
                    myLogger.Warn(e, comment:"connection usually brakes during refresh.");
                }
                finally
                {
                    await myLocks.Tasks.YieldTo(myLifetime, Scheduling.MainGuard);

                    myLogger.Verbose(
                            $"myPluginProtocolController.UnityModel.Value.Refresh.StartAsTask, force = {refreshType} Finished");
                    var solution = mySolution.GetProtocolSolution();
                    var solFolder = mySolution.SolutionDirectory;
                    var list = new List<string> {solFolder.FullPath};
                    myLogger.Verbose("RefreshPaths.StartAsTask Finished.");
                    await solution.GetFileSystemModel().RefreshPaths
                            .Start(lifetimeDef.Lifetime, new RdFsRefreshRequest(list, true)).AsTask();
                    await myLocks.Tasks.YieldTo(myLifetime, Scheduling.MainGuard);
                }
            }
            catch (Exception e)
            {
                myLogger.LogException(e);
            }
            finally
            {
                lifetimeDef.Terminate();
            }
        }
    }

    [SolutionComponent]
    public class UnityRefreshTracker
    {
        private readonly ILogger myLogger;
        private GroupingEvent myGroupingEvent;

        public UnityRefreshTracker(Lifetime lifetime, ISolution solution, UnityRefresher refresher,
            ILogger logger,
            IFileSystemTracker fileSystemTracker,
            FrontendBackendHost host,
            UnitySolutionTracker unitySolutionTracker)
        {
            myLogger = logger;
            if (solution.GetData(ProjectModelExtensions.ProtocolSolutionKey) == null)
                return;

            unitySolutionTracker.IsUnityProject.AdviseOnce(lifetime, args =>
            {
                if (!args) return;

                // Rgc.Guarded - beware RIDER-15577
                myGroupingEvent = solution.Locks.GroupingEvents.CreateEvent(lifetime, "UnityRefresherGroupingEvent",
                    TimeSpan.FromMilliseconds(500),
                    Rgc.Guarded, () =>
                    {
                        refresher.StartRefresh(RefreshType.Normal);
                    });

                host.Do(rd => rd.Refresh.Advise(lifetime, force =>
                    {
                        if (force)
                            refresher.StartRefresh(RefreshType.ForceRequestScriptReload);
                        else
                            myGroupingEvent.FireIncoming();
                    }));

                fileSystemTracker.RegisterPrioritySink(lifetime, FileSystemChange, HandlingPriority.Other);
            });
        }

        private void FileSystemChange(FileSystemChange fileSystemChange)
        {
            var visitor = new Visitor(this);
            foreach (var fileSystemChangeDelta in fileSystemChange.Deltas)
                fileSystemChangeDelta.Accept(visitor);
        }

        private void AdviseFileAddedOrDeleted(FileSystemChangeDelta delta)
        {
            if (delta.NewPath.ExtensionNoDot == "cs" || delta.NewPath.ExtensionNoDot == "asmdef")
            {
                myLogger.Verbose($"fileSystemTracker.AdviseDirectoryChanges {delta.ChangeType}, {delta.NewPath}, {delta.OldPath}");
                myGroupingEvent.FireIncoming();
            }
        }

        private class Visitor : RecursiveFileSystemChangeDeltaVisitor
        {
            private readonly UnityRefreshTracker myRefreshTracker;

            public Visitor(UnityRefreshTracker refreshTracker)
            {
                myRefreshTracker = refreshTracker;
            }

            public override void Visit(FileSystemChangeDelta delta)
            {
                base.Visit(delta);

                switch (delta.ChangeType)
                {
                    case FileSystemChangeType.ADDED:
                    case FileSystemChangeType.DELETED:
                        myRefreshTracker.AdviseFileAddedOrDeleted(delta);
                        break;
                    case FileSystemChangeType.CHANGED:
                    case FileSystemChangeType.RENAMED:
                    case FileSystemChangeType.UNKNOWN:
                    case FileSystemChangeType.SUBTREE_CHANGED:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}