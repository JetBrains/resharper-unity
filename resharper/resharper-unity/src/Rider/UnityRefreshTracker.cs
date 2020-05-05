using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Application.changes;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Host.Features.BackgroundTasks;
using JetBrains.ReSharper.Host.Features.FileSystem;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.Rider.Model;
using JetBrains.Threading;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityRefresher
    {
        private readonly IShellLocks myLocks;
        private readonly Lifetime myLifetime;
        private readonly ISolution mySolution;
        private readonly UnityEditorProtocol myEditorProtocol;
        private readonly ILogger myLogger;
        private readonly IContextBoundSettingsStoreLive myBoundSettingsStore;

        public UnityRefresher(IShellLocks locks, Lifetime lifetime, ISolution solution,
            UnityEditorProtocol editorProtocol, ISettingsStore settingsStore,
            ILogger logger)
        {
            myLocks = locks;
            myLifetime = lifetime;
            mySolution = solution;
            myEditorProtocol = editorProtocol;
            myLogger = logger;

            if (solution.GetData(ProjectModelExtensions.ProtocolSolutionKey) == null)
                return;

            myBoundSettingsStore =
                settingsStore.BindToContextLive(myLifetime, ContextRange.Smart(solution.ToDataContext()));
        }

        private RefreshType? mySecondaryRefreshType;
        private Task myRunningRefreshTask;

        public void StartRefresh(RefreshType refreshType)
        {
#pragma warning disable 4014
            Refresh(myLifetime, refreshType);
#pragma warning restore 4014
        }

        /// <summary>
        /// Calls Refresh in Unity, and RefreshPaths in vfs. If called multiple times while already running, schedules itself again
        /// </summary>
        /// <param name="lifetime"></param>
        /// <param name="refreshType"></param>
        public Task Refresh(Lifetime lifetime, RefreshType refreshType)
        {
            myLocks.AssertMainThread();
            myLocks.ReentrancyGuard.AssertGuarded();

            if (myEditorProtocol.UnityModel.Value == null)
                return Task.CompletedTask;

            if (!myBoundSettingsStore.GetValue((UnitySettings s) => s.AllowAutomaticRefreshInUnity) &&
                refreshType == RefreshType.Normal)
                return Task.CompletedTask;
            
            

            if (myRunningRefreshTask != null && !myRunningRefreshTask.IsCompleted)
            {
                myLogger.Verbose($"Secondary execution with {refreshType} type saved.");
                mySecondaryRefreshType = refreshType;
                return myRunningRefreshTask;
            }

            myRunningRefreshTask = RefreshInternal(lifetime, refreshType);
            return myRunningRefreshTask.ContinueWith(_ =>
            {
                myRunningRefreshTask = null;
                // if refresh signal came during execution preserve it and execute after finish
                if (mySecondaryRefreshType != null)
                {
                    myLogger.Verbose($"Secondary execution with {mySecondaryRefreshType}");
                    return Refresh(lifetime, (RefreshType) mySecondaryRefreshType)
                        .ContinueWith(___ => { mySecondaryRefreshType = null; }, lifetime);
                }

                return Task.CompletedTask;
            }, lifetime).Unwrap();
        }

        private async Task RefreshInternal(Lifetime lifetime, RefreshType refreshType)
        {
            var lifetimeDef = Lifetime.Define(lifetime);

            myLogger.Verbose($"myPluginProtocolController.UnityModel.Value.Refresh.StartAsTask, force = {refreshType} Started");
            mySolution.GetComponent<RiderBackgroundTaskHost>().AddNewTask(lifetimeDef.Lifetime,
                RiderBackgroundTaskBuilder.Create().WithHeader("Refreshing solution in Unity Editor...")
                    .AsIndeterminate().AsNonCancelable());
            try
            {
                var version = UnityVersion.Parse(myEditorProtocol.UnityModel.Value.UnityApplicationData.Value.ApplicationVersion);
                if (version != null && version.Major < 2018)
                {
                    var cookie = mySolution.GetComponent<VfsListener>().PauseChanges();
                    var task = RefreshAsTask(refreshType, lifetimeDef);
                    await task.ContinueWith(_ => { cookie.Dispose(); }, TaskContinuationOptions.ExecuteSynchronously); // RIDER-43222
                }
                else // it is a risk to pause vfs https://github.com/JetBrains/resharper-unity/issues/1601
                    await RefreshAsTask(refreshType, lifetimeDef);
            }
            catch (Exception e)
            {
                myLogger.LogException(e);
            }
            finally
            {
                myLogger.Verbose($"RefreshInternal Finished.");
                lifetimeDef.Terminate();
            }
        }

        private async Task RefreshAsTask(RefreshType refreshType, LifetimeDefinition lifetimeDef)
        {
            try
            {
                await myEditorProtocol.UnityModel.Value.Refresh.Start(lifetimeDef.Lifetime, refreshType).AsTask();
            }
            catch (Exception e)
            {
                myLogger.Warn("Connection usually brakes during refresh.", e);
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
            UnityHost host,
            UnitySolutionTracker unitySolutionTracker)
        {
            myLogger = logger;
            if (solution.GetData(ProjectModelExtensions.ProtocolSolutionKey) == null)
                return;

            unitySolutionTracker.IsUnityProjectFolder.AdviseOnce(lifetime, args =>
            {
                if (!args) return;
                
                // Rgc.Guarded - beware RIDER-15577
                myGroupingEvent = solution.Locks.GroupingEvents.CreateEvent(lifetime, "UnityRefresherGroupingEvent",
                    TimeSpan.FromMilliseconds(500),
                    Rgc.Guarded, () =>
                    {
                        refresher.StartRefresh(RefreshType.Normal);
                    });
                
                host.PerformModelAction(rd => rd.Refresh.Advise(lifetime, force =>
                    {
                        if (force)
                            refresher.StartRefresh(RefreshType.ForceRequestScriptReload);
                        else
                            myGroupingEvent.FireIncoming();
                    }));
            });

            unitySolutionTracker.IsUnityProject.AdviseOnce(lifetime, args =>
            {
                if (!args) return;

                var protocolSolution = solution.GetProtocolSolution();
                protocolSolution.Editors.AfterDocumentInEditorSaved.Advise(lifetime, _ =>
                {
                    logger.Verbose("protocolSolution.Editors.AfterDocumentInEditorSaved");
                    myGroupingEvent.FireIncoming();
                });
                
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
            if (delta.NewPath.ExtensionNoDot == "cs")
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