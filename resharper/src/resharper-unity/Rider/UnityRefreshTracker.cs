using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.DocumentModel;
using JetBrains.DocumentModel.Transactions;
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
            
            myPluginProtocolController.Refresh.Advise(lifetime, Refresh);
        }

        public bool IsRefreshing { get; private set; }

        public void Refresh(bool force)
        {
            myLocks.AssertMainThread();
            if (IsRefreshing)
                return;
            
            if (myPluginProtocolController.UnityModel.Value == null)
                return;
            
            if (!myBoundSettingsStore.GetValue((UnitySettings s) => s.AllowAutomaticRefreshInUnity) && !force)
                return;

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
        private readonly ILogger myLogger;

        public UnityRefreshTracker(Lifetime lifetime, ISolution solution, UnityRefresher refresher, 
            UnityEditorProtocol protocolController, DocumentTransactionManager documentTransactionManager, IShellLocks locks,
            ILogger logger)
        {
            myLogger = logger;
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
                
                myLogger.Verbose("protocolSolution.Editors.AfterDocumentInEditorSaved");
                groupingEvent.FireIncoming();
            });
            
            documentTransactionManager.AfterTransactionCommit.Advise(lifetime,
                args =>
                {
                    if (args.Succeded && args.Changes.Any())
                    {
                        locks.ExecuteWithReadLock(() =>
                        {
                            if (documentTransactionManager.CurrentTransaction?.ParentTransaction == null)
                            {
                                myLogger.Verbose("documentTransactionManager.AfterTransactionCommit");
                                groupingEvent.FireIncoming();
                            }
                        });    
                    }
                });
        }
    }
}