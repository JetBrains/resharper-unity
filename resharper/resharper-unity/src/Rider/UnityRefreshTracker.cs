using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Application.changes;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
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

            myBoundSettingsStore = settingsStore.BindToContextLive(myLifetime, ContextRange.Smart(solution.ToDataContext()));
        }

        private RefreshType? myRefreshType;
        private bool myIsRunning;

        /// <summary>
        /// Calls Refresh in Unity, and RefreshPaths in vfs. If called multiple times while already running, schedules itself again
        /// </summary>
        /// <param name="refreshType"></param>
        public async void Refresh(RefreshType refreshType)
        {
            myLocks.AssertMainThread();
            if (myEditorProtocol.UnityModel.Value == null)
                return;

            if (!myBoundSettingsStore.GetValue((UnitySettings s) => s.AllowAutomaticRefreshInUnity) && refreshType == RefreshType.Normal)
                return;

            if (myIsRunning)
            {
                myLogger.Verbose($"Secondary execution with {refreshType} type saved.");
                myRefreshType = refreshType;
                return;
            }

            myIsRunning = true;

            await RefreshInternal(refreshType);

            myIsRunning = false;

            if (myRefreshType == null)
                return;

            var type = myRefreshType.Value;
            myRefreshType = null;

            myLogger.Verbose($"Secondary execution with {type}");
            Refresh(type); // if refresh signal came during execution preserve it and execute after finish
        }

        private async Task RefreshInternal(RefreshType force)
        {
            var lifetimeDef = Lifetime.Define(myLifetime);

            myLogger.Verbose(
                $"myPluginProtocolController.UnityModel.Value.Refresh.StartAsTask, force = {force} Started");
            try
            {
                using (mySolution.GetComponent<VfsListener>().PauseChanges())
                {
                    await myEditorProtocol.UnityModel.Value.Refresh.Start(force).AsTask();
                }
                myLogger.Verbose(
                    $"myPluginProtocolController.UnityModel.Value.Refresh.StartAsTask, force = {force} Finished");

                var solution = mySolution.GetProtocolSolution();
                var solFolder = mySolution.SolutionDirectory;

                mySolution.GetComponent<RiderBackgroundTaskHost>().AddNewTask(lifetimeDef.Lifetime,
                    RiderBackgroundTaskBuilder.Create().WithHeader("Refreshing solution in Unity Editor...")
                        .AsIndeterminate().AsNonCancelable());

                var list = new List<string> {solFolder.FullPath};
                myLogger.Verbose($"RefreshPaths.StartAsTask Started.");
                await solution.GetFileSystemModel().RefreshPaths.Start(new RdRefreshRequest(list, true)).AsTask();
                myLogger.Verbose($"RefreshPaths.StartAsTask Finished.");

                myLogger.Verbose($"lifetimeDef.Terminate");
                lifetimeDef.Terminate();
            }
            catch (Exception e)
            {
                myLogger.Log(LoggingLevel.ERROR, "Exception during Refresh.", e);
            }
        }
    }

    [SolutionComponent]
    public class UnityRefreshTracker
    {
        private readonly UnityRefresher myRefresher;
        private readonly ILogger myLogger;
        private GroupingEvent myGroupingEvent;

        public UnityRefreshTracker(Lifetime lifetime, ISolution solution, UnityRefresher refresher,
            ILogger logger,
            IFileSystemTracker fileSystemTracker,
            UnityHost host,
            UnitySolutionTracker unitySolutionTracker)
        {
            myRefresher = refresher;
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
                        refresher.Refresh(RefreshType.Normal);
                    });

                host.PerformModelAction(rd => rd.Refresh.Advise(lifetime, force =>
                    {
                        if (force)
                            refresher.Refresh(RefreshType.ForceRequestScriptReload);
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