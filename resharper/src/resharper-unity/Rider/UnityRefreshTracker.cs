using System;
using System.Collections.Generic;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.Unity.Model;
using JetBrains.ProjectModel;
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
        private bool myIsRefreshing;

        public UnityRefresher(IShellLocks locks, Lifetime lifetime, ISolution solution)
        {
            myLocks = locks;
            myLifetime = lifetime;
            mySolution = solution;
        }

        public bool IsRefreshing => myIsRefreshing;

        public void Refresh(UnityModel unityModel)
        {
            myLocks.AssertMainThread();
            if (myIsRefreshing) return;

            myIsRefreshing = true;
            var result = unityModel?.Refresh.Start(RdVoid.Instance)?.Result;

            if (result == null)
            {
                myIsRefreshing = false;
                return;
            }
            
            var lifetimeDef = Lifetimes.Define(myLifetime);
            var solution = mySolution.GetProtocolSolution();
            var solFolder = mySolution.SolutionFilePath.Directory;
                
            mySolution.GetComponent<RiderBackgroundTaskHost>().AddNewTask(lifetimeDef.Lifetime, 
                RiderBackgroundTaskBuilder.Create().WithHeader("Refresh").AsIndeterminate().AsNonCancelable());
                        
            result.Advise(lifetimeDef.Lifetime, _ =>
            {
                try
                {
                    var list = new List<string> {solFolder.FullPath};
                    solution.FileSystemModel.RefreshPaths.Start(new RdRefreshRequest(list, true));
                }
                finally
                {
                    myIsRefreshing = false;
                    lifetimeDef.Terminate();
                }
            });
        }
    }

    [SolutionComponent]
    public class UnityRefreshTracker
    {
        public UnityRefreshTracker(Lifetime lifetime, ISolution solution, UnityRefresher refresher, UnityPluginProtocolController unityPluginProtocolController)
        {
            var groupingEvent = solution.Locks.GroupingEvents.CreateEvent(lifetime, "UnityRefresherOnSaveEvent", TimeSpan.FromMilliseconds(500),
                Rgc.Invariant, ()=>refresher.Refresh(unityPluginProtocolController.UnityModel));

            var protocolSolution = solution.GetProtocolSolution();
            protocolSolution.Editors.AfterDocumentInEditorSaved.Advise(lifetime, _ =>
            {
                if (refresher.IsRefreshing) return;
                //groupingEvent.FireIncoming();
            });
        }
    }
}