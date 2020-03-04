using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Collections;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Caches;
using JetBrains.ProjectModel.Tasks;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;
using JetBrains.Util.Threading.Tasks;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Caches
{
    [SolutionComponent]
    public class DeferredCacheController
    {
        private const int BATCH_SIZE = 5;

        private readonly Lifetime myLifetime;
        private readonly IPsiFiles myPsiFiles;
        private readonly SolutionAnalysisConfiguration mySolutionAnalysisConfiguration;
        private readonly IShellLocks myShellLocks;
        private readonly DeferredHelperCache myDeferredHelperCache;
        private readonly IEnumerable<IDeferredCache> myDeferredCaches;
        private readonly DeferredCacheProgressBar myProgressBar;
        private readonly ILogger myLogger;

        private readonly ConcurrentDictionary<IPsiSourceFile, Dictionary<IDeferredCache, object>> myCalculatedData = new ConcurrentDictionary<IPsiSourceFile, Dictionary<IDeferredCache, object>>();
        
        
        public IReadonlyProperty<bool> CompletedOnce => myCompletedOnce;
        private ViewableProperty<bool> myCompletedOnce;
        
        private Lifetime myCurrentBackgroundActivityLifetime = Lifetime.Terminated;
        private LifetimeDefinition myCurrentBackgroundActivityLifetimeDefinition;
        
        
        private readonly object myLockObject = new object();

        public DeferredCacheController(Lifetime lifetime, ISolution solution, SolutionCaches solutionCaches,
            ISolutionLoadTasksScheduler tasksScheduler, IPersistentIndexManager persistentIndexManager, IPsiFiles psiFiles,
            SolutionAnalysisConfiguration solutionAnalysisConfiguration, IShellLocks shellLocks,
            DeferredHelperCache deferredHelperCache, IEnumerable<IDeferredCache> deferredCaches,
            DeferredCacheProgressBar progressBar, ILogger logger)
        {
            myLifetime = lifetime;
            myPsiFiles = psiFiles;
            mySolutionAnalysisConfiguration = solutionAnalysisConfiguration;
            myShellLocks = shellLocks;
            myDeferredHelperCache = deferredHelperCache;
            myDeferredCaches = deferredCaches;
            myProgressBar = progressBar;
            myLogger = logger;
            myCompletedOnce = new ViewableProperty<bool>(false);
            
            
            tasksScheduler.EnqueueTask(new SolutionLoadTask("DeferredCacheControllerInitialization", SolutionLoadTaskKinds.AsLateAsPossible,
                () =>
                {
                    myShellLocks.QueueReadLock("DeferredCacheControllerInitializationRL", () =>
                    {
                        QueueCompletedOnce(solutionCaches.PersistentProperties.TryGetValue("DeferredCachesCompletedOnce", out var result) && result.Equals("True"));
            
                        myCompletedOnce.Advise(lifetime, b =>
                        {
                            solutionCaches.PersistentProperties["DeferredCachesCompletedOnce"] = b ? "True" : "False";
                            if (b)
                                solution.GetComponent<IDaemon>().Invalidate();
                        });
                    
                        myDeferredHelperCache.AfterAddToProcess.Advise(lifetime, _ =>
                        {
                            RunBackgroundActivity();
                        });
                    
                        myDeferredHelperCache.AfterRemoveFromProcess.Advise(lifetime, e =>
                        {
                            if (e.isDropped)
                            {
                                myCalculatedData.TryRemove(e.file, out _);
                            }
                            
                            RunBackgroundActivity();
                        });

                        RunBackgroundActivity();
                    });
                }));
        }


        private void RunBackgroundActivity()
        {
            if (!IsProcessingFiles())
            {
                if (!CompletedOnce.Value)
                    QueueCompletedOnce(true);
                
                return;
            }

            lock (myLockObject)
            {
                if (myCurrentBackgroundActivityLifetime.IsAlive)
                    return;

                myCurrentBackgroundActivityLifetimeDefinition = new LifetimeDefinition(myLifetime);
                myCurrentBackgroundActivityLifetime = myCurrentBackgroundActivityLifetimeDefinition.Lifetime;
            }
            

            myLogger.Verbose("Start processing files in deferred caches");
            mySolutionAnalysisConfiguration.Pause(myCurrentBackgroundActivityLifetime, "Calculating deferred index");
            myProgressBar.Start(myCurrentBackgroundActivityLifetime);
            
            myShellLocks.Tasks.StartNew(myLifetime, Scheduling.FreeThreaded, TaskPriority.Low, () => RunBackgroundActivityInner());
        }

        private void QueueCompletedOnce(bool result)
        {
            myShellLocks.Tasks.StartNew(myLifetime, Scheduling.MainGuard, TaskPriority.High, () => myCompletedOnce.Value = result);
        }

        private void RunBackgroundActivityInner()
        {
            if (!myLifetime.IsAlive)
                return;
            
            myShellLocks.QueueReadLock(myLifetime, "DeferredCacheControllerRunActivityInner", () =>
            {
                myPsiFiles.ExecuteAfterCommitAllDocuments(() =>
                {
                    myPsiFiles.AssertAllDocumentAreCommitted();
          
                    new InterruptableReadActivityThe(myLifetime, myShellLocks, () => !myLifetime.IsAlive || myShellLocks.ContentModelLocks.IsWriteLockRequested)
                    {
                        FuncRun = RunActivity,
                        FuncCancelled = RunBackgroundActivityInner,
                        FuncCompleted = () => { QueueCompletedOnce(true); }
                    }.DoStart();
                });
            });
        }

        public bool IsProcessingFiles()
        {
            return myDeferredHelperCache.FilesToDrop.Count > 0 || myDeferredHelperCache.FilesToProcess.Count > 0|| myCalculatedData.Count > 0;
        }

        public void RunActivity()
        {
            if (!myCurrentBackgroundActivityLifetime.IsAlive)
                return;
            
            var checker = new SeldomInterruptChecker();

            FlushBuildDataIfNeed();

            foreach (var psiSourceFile in myDeferredHelperCache.FilesToProcess)
            {
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

                myDeferredHelperCache.DropFromProcess(psiSourceFile, false);

                Assertion.Assert(!myCalculatedData.ContainsKey(psiSourceFile), "!myCalculatedData.ContainsKey(psiSourceFile)");
                myCalculatedData[psiSourceFile] = cacheToData;

                myLogger.Verbose("Build finished {0}", psiSourceFile.GetPersistentIdForLogging());

                FlushBuildDataIfNeed();
            }

            FlushBuildData();
            
            
            lock (myLockObject) {
                myCurrentBackgroundActivityLifetimeDefinition.Terminate();
                myCurrentBackgroundActivityLifetimeDefinition = null;
            }
        }

        private bool FlushBuildData()
        {
            if (myDeferredHelperCache.FilesToDrop.Count == 0 && myCalculatedData.Count == 0)
                return false;
            
            myShellLocks.Tasks.StartNew(myLifetime, Scheduling.MainGuard, () =>
            {
                using (WriteLockCookie.Create())
                {
                    var files = 0;
                    foreach (var sourceFile in myDeferredHelperCache.FilesToDrop)
                    {
                        myLogger.Verbose("Start dropping {0}", sourceFile.GetPersistentIdForLogging());

                        if (files > BATCH_SIZE)
                            break;

                        foreach (var deferredCache in myDeferredCaches)
                        {
                            try
                            {
                                deferredCache.Drop(sourceFile);
                            }
                            catch (Exception e)
                            {
                                myLogger.Error(e, "An error occurred during dropping data in cache {0}", myDeferredCaches.GetType().Name);
                            }
                        }

                        myDeferredHelperCache.FilesToDrop.Remove(sourceFile);
                        myLogger.Verbose("Finish dropping {0}", sourceFile.GetPersistentIdForLogging());
                        files++;
                    }

                    foreach (var sourceFile in myCalculatedData.Keys)
                    {
                        if (files > BATCH_SIZE)
                            break;

                        myLogger.Verbose("Start merging for {0}", sourceFile);
                        var cacheToData = myCalculatedData[sourceFile];

                        foreach (var (cache, data) in cacheToData)
                        {
                            try
                            {
                                cache.Merge(sourceFile, data);
                            }
                            catch (Exception e)
                            {
                                myLogger.Error(e, "An error occurred during merging data to cache {0}", cache.GetType().Name);
                            }
                        }


                        myCalculatedData.TryRemove(sourceFile, out _);

                        myLogger.Verbose("Finish merging for {0}", sourceFile);
                        files++;
                    }
                }
            });

            return true;
        }

        private bool FlushBuildDataIfNeed()
        {
            if (myCalculatedData.Count + myDeferredHelperCache.FilesToDrop.Count > BATCH_SIZE)
            {
                FlushBuildData();
                throw new OperationCanceledException();
            }

            return false;
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
        
    }
}