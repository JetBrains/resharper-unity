using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        private readonly IContextBoundSettingsStoreLive myBoundSettingsStore;

        public UnityRefresher(IShellLocks locks, Lifetime lifetime, ISolution solution, 
            UnityEditorProtocol pluginProtocolController, ISettingsStore settingsStore)
        {
            myLocks = locks;
            myLifetime = lifetime;
            mySolution = solution;
            myPluginProtocolController = pluginProtocolController;
            
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

        public UnityRefreshTracker(Lifetime lifetime, ISolution solution, UnityRefresher refresher, 
            UnityEditorProtocol protocolController,
            ILogger logger)
        {
            myLogger = logger;
            
            if (solution.GetData(ProjectModelExtensions.ProtocolSolutionKey) == null)
                return;
                       
            var groupingEvent = solution.Locks.GroupingEvents.CreateEvent(lifetime, "UnityRefresherOnSaveEvent", TimeSpan.FromMilliseconds(500),
                Rgc.Invariant, ()=> refresher.Refresh(false));

            var protocolSolution = solution.GetProtocolSolution();
            protocolSolution.Editors.AfterDocumentInEditorSaved.Advise(lifetime, _ =>
            {
                if (protocolController.UnityModel.Value == null)
                    return;
                
                myLogger.Verbose("protocolSolution.Editors.AfterDocumentInEditorSaved");
                groupingEvent.FireIncoming();
            });
        }
    }
}