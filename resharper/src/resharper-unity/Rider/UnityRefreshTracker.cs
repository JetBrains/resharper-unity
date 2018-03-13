using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.changes;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.Platform.Unity.Model;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.ProjectsHost.Impl;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Host.Features.BackgroundTasks;
using JetBrains.Rider.Model;
using JetBrains.Threading;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityRefresher
    {
        private readonly IShellLocks myLocks;
        private readonly Lifetime myLifetime;
        private readonly ISolution mySolution;
        private readonly UnityEditorProtocol myPluginProtocolController;

        public UnityRefresher(IShellLocks locks, Lifetime lifetime, ISolution solution, UnityEditorProtocol pluginProtocolController)
        {
            myLocks = locks;
            myLifetime = lifetime;
            mySolution = solution;
            myPluginProtocolController = pluginProtocolController;
            
            if (solution.GetData(ProjectModelExtensions.ProtocolSolutionKey) == null)
                return;
                        
            myPluginProtocolController.Refresh.Advise(lifetime, model => { Refresh(model.Force); });
        }

        public bool IsRefreshing { get; private set; }

        public void Refresh(bool force)
        {
            myLocks.AssertMainThread();
            if (IsRefreshing) return;

            IsRefreshing = true;
            var result = myPluginProtocolController.UnityModel.Value.Refresh.Start(force)?.Result;

            if (result == null)
            {
                IsRefreshing = false;
                return;
            }
            
            var lifetimeDef = Lifetimes.Define(myLifetime);
            var solution = mySolution.GetProtocolSolution();
            var solFolder = mySolution.SolutionFilePath.Directory;
                
            mySolution.GetComponent<RiderBackgroundTaskHost>().AddNewTask(lifetimeDef.Lifetime, 
                RiderBackgroundTaskBuilder.Create().WithHeader("Refreshing solution in Unity Editor...").AsIndeterminate().AsNonCancelable());
                        
            result.Advise(lifetimeDef.Lifetime, _ =>
            {
                try
                {
                    var list = new List<string> {solFolder.FullPath};
                    solution.GetFileSystemModel().RefreshPaths.Start(new RdRefreshRequest(list, true));
                }
                finally
                {
                    IsRefreshing = false;
                    lifetimeDef.Terminate();
                }
            });
        }
    }

    [SolutionComponent]
    public class UnityRefreshTracker
    {
        public UnityRefreshTracker(Lifetime lifetime, ISolution solution, UnityRefresher refresher, ChangeManager changeManager, UnityEditorProtocol protocolController)
        {
            if (solution.GetData(ProjectModelExtensions.ProtocolSolutionKey) == null)
                return;
            
            var groupingEvent = solution.Locks.GroupingEvents.CreateEvent(lifetime, "UnityRefresherOnSaveEvent", TimeSpan.FromMilliseconds(500),
                Rgc.Invariant, ()=>refresher.Refresh(false));

            var protocolSolution = solution.GetProtocolSolution();
            protocolSolution.Editors.AfterDocumentInEditorSaved.Advise(lifetime, _ =>
            {
                if (refresher.IsRefreshing) return;

                if (protocolController.UnityModel.Value == null)
                    return;
                
                var isPlay = protocolController.UnityModel.Value.Play.HasTrueValue();
                if (isPlay) return;
                
                groupingEvent.FireIncoming();
            });

            changeManager.Changed2.Advise(lifetime, args =>
            {
                var changes = args.ChangeMap.GetChanges<ProjectModelChange>();
                if (changes == null)
                    return;
                
                if (refresher.IsRefreshing) 
                    return;

                var hasChange = changes.Any(HasAnyFileChangeRec);
                if (!hasChange)
                    return;
                
                var isPlay = protocolController.UnityModel.Value?.Play.HasTrueValue();
                if (isPlay == null || (bool)isPlay) 
                    return;

                groupingEvent.FireIncoming();
            });
        }

        private bool HasAnyFileChangeRec(ProjectModelChange change)
        {
            var file = change.ProjectModelElement as IProjectFile;

            if (file != null && (change.IsAdded || change.IsRemoved || change.IsMovedIn || change.IsMovedOut))
            {
                // Log something
                return true;
            }

            foreach (var childChange in change.GetChildren())
            {
                if (HasAnyFileChangeRec(childChange))
                {
                    return true;
                }
            }
            return false;
        }
    }
}