using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Collections;
using JetBrains.Collections.Synchronized;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Caches;
using JetBrains.ProjectModel.Tasks;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon.Experimental;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Threading;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Caches
{
    [SolutionComponent]
    public class DeferredCacheController : IDaemonTaskBeforeInvisibleProcessProvider
    {
        private const int BATCH_SIZE = 10;

        private readonly Lifetime myLifetime;
        private readonly ISolution mySolution;
        private readonly SolutionCaches mySolutionCaches;
        private readonly IPsiFiles myPsiFiles;
        private readonly SolutionAnalysisConfiguration mySolutionAnalysisConfiguration;
        private readonly IShellLocks myShellLocks;
        private readonly DeferredHelperCache myDeferredHelperCache;
        private readonly IEnumerable<IDeferredCache> myDeferredCaches;
        private readonly DeferredCacheProgressBar myProgressBar;
        private readonly ILogger myLogger;
        private GroupingEvent myGroupingEvent;

        public IReadonlyProperty<bool> CompletedOnce => myCompletedOnce;
        private readonly ViewableProperty<bool> myCompletedOnce;
        
        private Lifetime myCurrentBackgroundActivityLifetime = Lifetime.Terminated;
        private LifetimeDefinition myCurrentBackgroundActivityLifetimeDefinition;

        public DeferredCacheController(Lifetime lifetime, ISolution solution, SolutionCaches solutionCaches,
            ISolutionLoadTasksScheduler tasksScheduler, IPersistentIndexManager persistentIndexManager, IPsiFiles psiFiles,
            SolutionAnalysisConfiguration solutionAnalysisConfiguration, IShellLocks shellLocks,
            DeferredHelperCache deferredHelperCache, IEnumerable<IDeferredCache> deferredCaches,
            DeferredCacheProgressBar progressBar, ILogger logger)
        {
            myLifetime = lifetime;
            mySolution = solution;
            mySolutionCaches = solutionCaches;
            myPsiFiles = psiFiles;
            mySolutionAnalysisConfiguration = solutionAnalysisConfiguration;
            myShellLocks = shellLocks;
            myDeferredHelperCache = deferredHelperCache;
            myDeferredCaches = deferredCaches;
            myProgressBar = progressBar;
            myLogger = logger;
            var defaultValue = solutionCaches.PersistentProperties.TryGetValue("DeferredCachesCompletedOnce", out var result) && result.Equals("True");
            myCompletedOnce = new ViewableProperty<bool>(defaultValue);
            
            myGroupingEvent = solution.Locks.GroupingEvents.CreateEvent(lifetime, "DeferredCachesCoreActivity",  TimeSpan.FromMilliseconds(500), Rgc.Guarded, RunBackgroundActivity);
        }

        private void ScheduleBackgroundActivity()
        {
            myShellLocks.Dispatcher.AssertAccess();
            if (myCurrentBackgroundActivityLifetime.IsAlive)
                return;
            
            myCurrentBackgroundActivityLifetimeDefinition = new LifetimeDefinition(myLifetime);
            myCurrentBackgroundActivityLifetime = myCurrentBackgroundActivityLifetimeDefinition.Lifetime;
            
            mySolutionAnalysisConfiguration.Pause(myCurrentBackgroundActivityLifetime, "Calculating deferred index");
            myProgressBar.Start(myCurrentBackgroundActivityLifetime);
            myIsProcessing = true;
            myLogger.Verbose("Start processing files in deferred caches");

            myGroupingEvent.FireIncoming();
        }

        //Start
        private volatile bool myIsProcessing;
        private void RunBackgroundActivity()
        {
            myShellLocks.Dispatcher.AssertAccess();

            using (ReadLockCookie.Create())
            {
                Assertion.Assert(myCurrentBackgroundActivityLifetime.IsAlive, "myCurrentBackgroundActivityLifetime.IsAlive");
                if (HasDirtyFiles())
                {
                    var filesToDelete = new SynchronizedList<IPsiSourceFile>(myDeferredHelperCache.FilesToDrop.Take(BATCH_SIZE));
                    var filesToAdd = new SynchronizedList<IPsiSourceFile>(myDeferredHelperCache.FilesToProcess.Take(BATCH_SIZE));
                
                    var calculatedData = new ConcurrentDictionary<IPsiSourceFile, (long, Dictionary<IDeferredCache, object>)>();
                    ScheduleBackgroundProcess(filesToDelete, filesToAdd, calculatedData);
                }
                else
                {
                    myCurrentBackgroundActivityLifetimeDefinition.Terminate();
                    myCurrentBackgroundActivityLifetimeDefinition = null;
                    myCompletedOnce.Value = true;
                    myIsProcessing = false;
                    myLogger.Verbose("Finish processing files in deferred caches");

                    mySolution.GetComponent<IDaemon>().Invalidate();
                }
            }
        }

        private void ScheduleBackgroundProcess(SynchronizedList<IPsiSourceFile> toDelete, SynchronizedList<IPsiSourceFile> toProcess,
            ConcurrentDictionary<IPsiSourceFile, (long, Dictionary<IDeferredCache, object>)> calculatedData)
        {
            myShellLocks.Dispatcher.AssertAccess();
            
            myPsiFiles.ExecuteAfterCommitAllDocuments(() =>
            {
                myPsiFiles.AssertAllDocumentAreCommitted();
          
                new InterruptableReadActivityThe(myLifetime, myShellLocks, () => !myLifetime.IsAlive || myShellLocks.ContentModelLocks.IsWriteLockRequested)
                {
                    FuncRun = () => RunActivity(toDelete, toProcess, calculatedData),
                    FuncCancelled = () => myShellLocks.Tasks.StartNew(myLifetime, Scheduling.MainGuard, () => ScheduleBackgroundProcess(toDelete, toProcess, calculatedData)),
                    FuncCompleted = () =>
                    {
                        myShellLocks.Dispatcher.AssertAccess();
                        FlushData(toDelete, toProcess, calculatedData);
                    }
                }.DoStart();
            });
        }
        
        public bool HasDirtyFiles()
        {
            return myDeferredHelperCache.FilesToDrop.Count > 0 || myDeferredHelperCache.FilesToProcess.Count > 0;
        }

        public bool IsProcessingFiles()
        {
            return myIsProcessing;
        }

        // background tread
        private void RunActivity(SynchronizedList<IPsiSourceFile> toDelete, SynchronizedList<IPsiSourceFile> toProcess,
            ConcurrentDictionary<IPsiSourceFile, (long, Dictionary<IDeferredCache, object>)> calculatedData)
        {
            myShellLocks.AssertReadAccessAllowed();
            var checker = new SeldomInterruptChecker();
            foreach (var psiSourceFile in toProcess)
            {
                if (!psiSourceFile.GetLocation().ExistsFile)
                {
                    Assertion.Assert(!myDeferredHelperCache.FilesToProcess.Contains(psiSourceFile), "!myDeferredHelperCache.FilesToProcess.Contains(psiSourceFile)");
                    toProcess.Remove(psiSourceFile);
                }
                
                if (!psiSourceFile.IsValid())
                {
                    // file could be dropped, because we are working with snapshot, do not call build for invalid files
                    Assertion.Assert(!myDeferredHelperCache.FilesToProcess.Contains(psiSourceFile), "!myDeferredHelperCache.FilesToProcess.Contains(psiSourceFile)");
                    toProcess.Remove(psiSourceFile);
                    calculatedData.TryRemove(psiSourceFile, out var _);
                    continue;
                }
                
                if (calculatedData.TryGetValue(psiSourceFile, out var result))
                {
                    if (result.Item1 == psiSourceFile.GetAggregatedTimestamp())
                        continue;
                    
                    // drop unactual data

                    calculatedData.TryRemove(psiSourceFile, out _);
                }

                myProgressBar.SetCurrentProcessingFile(psiSourceFile);
                myLogger.Verbose("Build started {0}", psiSourceFile.GetPersistentIdForLogging());
                var cacheToData = new Dictionary<IDeferredCache, object>();

                foreach (var cache in myDeferredCaches)
                {
                    if (!cache.IsApplicable(psiSourceFile))
                        continue;
                    
                    if (cache.UpToDate(psiSourceFile))
                        continue;

                    try
                    {
                        cacheToData[cache] = cache.Build(psiSourceFile);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        myLogger.Error(e, "An error occurred during build cache {0}", cache.GetType().Name);
                    }

                    checker.CheckForInterrupt();
                }

                Assertion.Assert(!calculatedData.ContainsKey(psiSourceFile), "!myCalculatedData.ContainsKey(psiSourceFile)");
                calculatedData[psiSourceFile] = (psiSourceFile.GetAggregatedTimestamp(), cacheToData);

                myLogger.Verbose("Build finished {0}", psiSourceFile.GetPersistentIdForLogging());
            }
        }

        private void FlushData(SynchronizedList<IPsiSourceFile> toDelete, SynchronizedList<IPsiSourceFile> toProcess,
            ConcurrentDictionary<IPsiSourceFile, (long, Dictionary<IDeferredCache, object>)> calculatedData)
        {
            myShellLocks.Dispatcher.AssertAccess();

            using (WriteLockCookie.Create())
            {
                using (myLogger.StopwatchCookie("DeferredCachesFlushData"))
                {
                    // TODO : assert that toProcess == calculatedData.Keys
                    foreach (var sourceFile in toProcess)
                    {
                        // out toProcess is only snapshot and could be not actual, if file was removed, we could skip merging
                        if (myDeferredHelperCache.FilesToDrop.Contains(sourceFile))
                        {
                            Assertion.Assert(!myDeferredHelperCache.FilesToProcess.Contains(sourceFile),
                                "!myDeferredHelperCache.FilesToProcess.Contains(sourceFile)");
                            calculatedData.TryRemove(sourceFile, out _);
                            toProcess.Remove(sourceFile);
                            continue;
                        }

                        Assertion.Assert(myDeferredHelperCache.FilesToProcess.Contains(sourceFile),
                            "myDeferredHelperCache.FilesToProcess.Contains(sourceFile)");

                        var (timeStamp, cacheToData) = calculatedData[sourceFile];

                        // out calculated data is out-of-date
                        if (timeStamp != sourceFile.GetAggregatedTimestamp())
                        {
                            calculatedData.TryRemove(sourceFile, out _);
                            toProcess.Remove(sourceFile);
                            continue;
                        }

                        foreach (var (cache, data) in cacheToData)
                        {
                            try
                            {
                                cache.Merge(sourceFile, data);
                            }
                            catch (Exception e)
                            {
                                myLogger.Error(e, "An error occurred during merging data to cache {0}",
                                    cache.GetType().Name);
                            }
                        }

                        myDeferredHelperCache.DropFromProcess(sourceFile);
                        calculatedData.TryRemove(sourceFile, out _);
                        toProcess.Remove(sourceFile);
                    }

                    foreach (var sourceFile in toDelete)
                    {
                        // toDelete is only snapshot and could be not actual, if file was added again, we could skip dropping for optimization
                        if (myDeferredHelperCache.FilesToProcess.Contains(sourceFile))
                        {
                            Assertion.Assert(!myDeferredHelperCache.FilesToDrop.Contains(sourceFile),
                                "!myDeferredHelperCache.FilesToDrop.Contains(sourceFile)");
                            toDelete.Remove(sourceFile);
                            continue;
                        }

                        Assertion.Assert(myDeferredHelperCache.FilesToDrop.Contains(sourceFile),
                            "myDeferredHelperCache.FilesToDrop.Contains(sourceFile)");


                        foreach (var cache in myDeferredCaches)
                        {
                            try
                            {
                                cache.Drop(sourceFile);
                            }
                            catch (Exception e)
                            {
                                myLogger.Error(e, "An error occurred during merging data to cache {0}",
                                    cache.GetType().Name);
                            }
                        }

                        myDeferredHelperCache.FilesToDrop.Remove(sourceFile);
                        toDelete.Remove(sourceFile);
                    }
                }
            }

            // requeue for next part
            myShellLocks.Tasks.StartNew(myLifetime, Scheduling.MainGuard, () => { RunBackgroundActivity();});
        }


        public void Invalidate<T>() where T : IDeferredCache
        {
            myShellLocks.AssertWriteAccessAllowed();
            
            foreach (var deferredCache in myDeferredCaches)
            {
                if (deferredCache is T)
                    deferredCache.Invalidate();
            }
        }

        private volatile bool myIsFirstTime = true;

        public Action CreateTask(Lifetime lifetime)
        {
            if (myIsFirstTime)
            {
                myShellLocks.ExecuteOrQueueEx(myLifetime, "DeferredCacheInitialization", () =>
                {
                    myIsFirstTime = false;
                    myCompletedOnce.Change.AdviseOnce(myLifetime, b =>
                    {
                        mySolutionCaches.PersistentProperties["DeferredCachesCompletedOnce"] = b ? "True" : "False";
                    });

                    myDeferredHelperCache.AfterAddToProcess.Advise(myLifetime, _ => ScheduleBackgroundActivity());
                    myDeferredHelperCache.AfterRemoveFromProcess.Advise(myLifetime, _ => ScheduleBackgroundActivity());
                    
                    ScheduleBackgroundActivity();

                });
            }

            return () => { };
        }
    }
}