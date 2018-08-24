using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Application.changes;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework.Tasks;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Host.Features.BackgroundTasks;
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
        private readonly UnityEditorProtocol myPluginProtocolController;
        private readonly ILogger myLogger;
        private readonly IContextBoundSettingsStoreLive myBoundSettingsStore;

        public UnityRefresher(IShellLocks locks, Lifetime lifetime, ISolution solution, 
            UnityEditorProtocol pluginProtocolController, ISettingsStore settingsStore,
            ILogger logger)
        {
            myLocks = locks;
            myLifetime = lifetime;
            mySolution = solution;
            myPluginProtocolController = pluginProtocolController;
            myLogger = logger;

            if (solution.GetData(ProjectModelExtensions.ProtocolSolutionKey) == null)
                return;
                        
            myBoundSettingsStore = settingsStore.BindToContextLive(myLifetime, ContextRange.Smart(solution.ToDataContext()));
            
            myPluginProtocolController.Refresh.Advise(lifetime, b => { Refresh(b); });
        }

        private Task CurrentTask;

        public Task Refresh(bool force)
        {
            myLocks.AssertMainThread();
            if (CurrentTask != null)
                return CurrentTask;

            if (myPluginProtocolController.UnityModel.Value == null)
                return new Task(()=>{});

            if (!myBoundSettingsStore.GetValue((UnitySettings s) => s.AllowAutomaticRefreshInUnity) && !force)
                return new Task(()=>{});

            myLogger.Verbose($"myPluginProtocolController.UnityModel.Value.Refresh.StartAsTask, force = {force}");
            var task = myPluginProtocolController.UnityModel.Value.Refresh.StartAsTask(force);
            CurrentTask = task;
            
            var lifetimeDef = Lifetimes.Define(myLifetime);
            var solution = mySolution.GetProtocolSolution();
            var solFolder = mySolution.SolutionFilePath.Directory;
                
            mySolution.GetComponent<RiderBackgroundTaskHost>().AddNewTask(lifetimeDef.Lifetime, 
                RiderBackgroundTaskBuilder.Create().WithHeader("Refreshing solution in Unity Editor...").AsIndeterminate().AsNonCancelable());
                        
            task.ContinueWith(_ =>
            {
                mySolution.Locks.ExecuteOrQueueEx(lifetimeDef.Lifetime, "RefreshPaths", () =>
                {
                    try
                    {
                        var list = new List<string> {solFolder.FullPath};
                        solution.GetFileSystemModel().RefreshPaths.Start(new RdRefreshRequest(list, true));
                    }
                    finally
                    {
                        CurrentTask = null;
                        lifetimeDef.Terminate();
                    }
                });
            });
            return task;
        }
    }

    [SolutionComponent]
    public class UnityRefreshTracker
    {
        private readonly ILogger myLogger;
        private readonly GroupingEvent myGroupingEvent;

        public UnityRefreshTracker(Lifetime lifetime, ISolution solution, UnityRefresher refresher, 
            UnityEditorProtocol protocolController,
            ILogger logger,
            IFileSystemTracker fileSystemTracker)
        {
            myLogger = logger;

            if (solution.GetData(ProjectModelExtensions.ProtocolSolutionKey) == null)
                return;
            
            // Rgc.Guarded - beware RIDER-15577
            myGroupingEvent = solution.Locks.GroupingEvents.CreateEvent(lifetime, "UnityRefresherOnSaveEvent", TimeSpan.FromMilliseconds(500),
                Rgc.Guarded, ()=> refresher.Refresh(false));

            var protocolSolution = solution.GetProtocolSolution();
            protocolSolution.Editors.AfterDocumentInEditorSaved.Advise(lifetime, _ =>
            {
                if (protocolController.UnityModel.Value == null)
                    return;
                
                myLogger.Verbose("protocolSolution.Editors.AfterDocumentInEditorSaved");
                myGroupingEvent.FireIncoming();
            });
            
            fileSystemTracker.RegisterPrioritySink(lifetime, FileSystemChange, HandlingPriority.Other);
        }
        
        private void FileSystemChange(FileSystemChange fileSystemChange)
        {
            var visitor = new Visitor(this);
            foreach (var fileSystemChangeDelta in fileSystemChange.Deltas)
                fileSystemChangeDelta.Accept(visitor);
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

        private void AdviseFileAddedOrDeleted(FileSystemChangeDelta delta)
        {
            if (delta.NewPath.ExtensionNoDot == "cs")
            {
                myLogger.Verbose($"fileSystemTracker.AdviseDirectoryChanges {delta.ChangeType}, {delta.NewPath}, {delta.OldPath}");
                myGroupingEvent.FireIncoming();
            }
        }
    }
}