using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Caches;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon.Experimental;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Caches
{
    [SolutionComponent]
    public class DeferredCacheController : IDaemonTaskBeforeInvisibleProcessProvider
    {
        private const int BATCH_SIZE = 5;

        private readonly ISolution mySolution;
        private readonly SolutionAnalysisConfiguration mySolutionAnalysisConfiguration;
        private readonly IShellLocks myShellLocks;
        private readonly DeferredCachesLocks myDeferredCachesLocks;
        private readonly DeferredHelperCache myDeferredHelperCache;
        private readonly IEnumerable<IDeferredCache> myDeferredCaches;
        private readonly DeferredCacheProgressBar myProgressBar;
        private readonly ILogger myLogger;

        private readonly ConcurrentDictionary<IPsiSourceFile, Dictionary<IDeferredCache, object>> myPartlyCalculatedData = new ConcurrentDictionary<IPsiSourceFile, Dictionary<IDeferredCache, object>>();
        private readonly ConcurrentDictionary<IPsiSourceFile, Dictionary<IDeferredCache, object>> myCalculatedData = new ConcurrentDictionary<IPsiSourceFile, Dictionary<IDeferredCache, object>>();
        private readonly SequentialLifetimes myWorkLifetime;
        public IReadonlyProperty<bool> CompletedOnce => myCompletedOnce;
        private ViewableProperty<bool> myCompletedOnce;
        
        public DeferredCacheController(Lifetime lifetime, ISolution solution, SolutionCaches solutionCaches, IPersistentIndexManager persistentIndexManager, SolutionAnalysisConfiguration solutionAnalysisConfiguration, IShellLocks shellLocks, DeferredCachesLocks deferredCachesLocks,
            DeferredHelperCache deferredHelperCache, IEnumerable<IDeferredCache> deferredCaches, DeferredCacheProgressBar progressBar, ILogger logger)
        {
            mySolution = solution;
            mySolutionAnalysisConfiguration = solutionAnalysisConfiguration;
            myShellLocks = shellLocks;
            myDeferredCachesLocks = deferredCachesLocks;
            myDeferredHelperCache = deferredHelperCache;
            myDeferredCaches = deferredCaches;
            myProgressBar = progressBar;
            myLogger = logger;
            myWorkLifetime = new SequentialLifetimes(lifetime);
            myCompletedOnce = new ViewableProperty<bool>();
            
            if (solutionCaches.PersistentProperties.TryGetValue("DeferredCachesCompletedOnce", out var result))
            {
                myCompletedOnce.Value = result.Equals("True");
            }
            else
            {
                myCompletedOnce.Value = false;
            }
            
            myCompletedOnce.Change.Advise(lifetime, b =>
            {
                solutionCaches.PersistentProperties["DeferredCachesCompletedOnce"] = b ? "True" : "False";
            });
        }

        public bool IsProcessingFiles()
        {
            return myDeferredHelperCache.FilesToDrop.Count > 0 || myDeferredHelperCache.FilesToProcess.Count > 0 ||
                   myPartlyCalculatedData.Count > 0 || myCalculatedData.Count > 0;
        }

        public Action CreateTask(Lifetime lifetime)
        {
            return () =>
            {
                using (TryReadLockCookie.Create(NullProgressIndicator.Instance, myShellLocks, () => !lifetime.IsAlive))
                {
                    if (!IsProcessingFiles())
                    {
                        myCompletedOnce.Value = true;
                        return;
                    }
                    
                    if (myWorkLifetime.IsCurrentTerminated)
                    {
                        var workLifetime = myWorkLifetime.Next();
                        myLogger.Verbose("Start processing files in deferred caches");
                        if (!myCompletedOnce.Value)
                        {
                            mySolutionAnalysisConfiguration.Pause(workLifetime, "Calculating deferred index");
                        }
                        
                        myProgressBar.Start(workLifetime);
                    }
                    
                    // First of all, drop data for out-of-date files under DeferredCachesWriteLock
                    foreach (var psiSourceFile in new LocalList<IPsiSourceFile>(myDeferredHelperCache.FilesToDrop))
                    {
                        myLogger.Verbose("Drop {0}", psiSourceFile.GetPersistentIdForLogging());
                        myPartlyCalculatedData.TryRemove(psiSourceFile, out _);
                        myCalculatedData.TryRemove(psiSourceFile, out _);
                        
                        myDeferredCachesLocks.ExecuteUnderWriteLock(() =>
                        {
                            // Drop operation should be fast, do not interrupt here
                            foreach (var cache in myDeferredCaches)
                            {
                                try
                                {
                                    cache.Drop(psiSourceFile);
                                }
                                catch (Exception e)
                                {
                                    myLogger.Error(e, "An error occurred during dropping data in cache {0}", cache.GetType().Name);
                                }
                            }
                        });
                        
                        // Tidy up dropped file from collection
                        myDeferredHelperCache.FilesToDrop.Remove(psiSourceFile);
                        
                        
                        // That point is safe for checking interruption
                        CheckForInterrupt(lifetime);
                    }

                    myDeferredHelperCache.FilesToDrop.Clear();

                    foreach (var psiSourceFile in GetFilesToProcess())
                    {
                        myCalculatedData.TryRemove(psiSourceFile, out _);
                    }
                    
                    // Possibly, there was interruption in previous flush, prioritize data flushing
                    FlushBuildDataIfNeed(lifetime);

                    foreach (var psiSourceFile in GetFilesToProcess())
                    {
                        myProgressBar.SetCurrentProcessingFile(psiSourceFile);
                        myLogger.Verbose("Build started {0}", psiSourceFile.GetPersistentIdForLogging());
                        Assertion.Assert(psiSourceFile.IsValid(), "psiSourceFile.IsValid()");
                        if (!myPartlyCalculatedData.TryGetValue(psiSourceFile, out var cacheToData))
                        {
                            cacheToData = new Dictionary<IDeferredCache, object>();
                            myPartlyCalculatedData[psiSourceFile] = cacheToData;
                        }

                        myDeferredCachesLocks.ExecuteUnderReadLock(_ =>
                        {
                            foreach (var cache in myDeferredCaches)
                            {
                                if (!cache.IsApplicable(psiSourceFile))
                                    continue;
                            
                                if (cache.UpToDate(psiSourceFile))
                                    continue;
                            
                                if (cacheToData.ContainsKey(cache))
                                    continue;


                                try
                                {
                                    cacheToData[cache] = cache.Build(lifetime, psiSourceFile);
                                }
                                catch (OperationCanceledException)
                                {
                                    throw;
                                }
                                catch (Exception e)
                                {
                                    myLogger.Error(e, "An error occurred during build cache {0}", cache.GetType().Name);
                                }

                                CheckForInterrupt(lifetime);
                            }
                        });

                        myDeferredHelperCache.DropFromProcess(psiSourceFile);

                        Assertion.Assert(!myCalculatedData.ContainsKey(psiSourceFile),
                            "!myCalculatedData.ContainsKey(psiSourceFile)");
                        myCalculatedData[psiSourceFile] = myPartlyCalculatedData[psiSourceFile];
                        myPartlyCalculatedData.TryRemove(psiSourceFile, out _);

                        FlushBuildDataIfNeed(lifetime);
                        myLogger.Verbose("Build finished {0}", psiSourceFile.GetPersistentIdForLogging());

                    }

                    FlushBuildData(lifetime);
                    
                    myWorkLifetime.TerminateCurrent();
                    myCompletedOnce.Value = true;
                    
                    // invalidate and update highlightings according to deferred caches data
                    mySolution.GetComponent<IDaemon>().Invalidate();
                }
            };
        }

        private void CheckForInterrupt(Lifetime lifetime)
        {
            if (!lifetime.IsAlive)
                throw new OperationCanceledException();
        }

        private IEnumerable<IPsiSourceFile> GetFilesToProcess()
        {
            // prioritize cache calculation for files, which we have started to process already
            foreach (var sourceFile in new LocalList<IPsiSourceFile>(myPartlyCalculatedData.Keys))
            {
                yield return sourceFile;
            }

            foreach (var sourceFile in myDeferredHelperCache.FilesToProcess)
            {
                yield return sourceFile;
            }
        }

        private void FlushBuildData(Lifetime lifetime)
        {

            foreach (var sourceFile in new LocalList<IPsiSourceFile>(myCalculatedData.Keys))
            {
                CheckForInterrupt(lifetime);
                myLogger.Verbose("Start merging for {0}", sourceFile);
                var cacheToData = myCalculatedData[sourceFile];
                myDeferredCachesLocks.ExecuteUnderWriteLock(() =>
                {
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
                });

                myCalculatedData.TryRemove(sourceFile, out _);

                myLogger.Verbose("Finish merging for {0}", sourceFile);
            }
        }

        private void FlushBuildDataIfNeed(Lifetime lifetime)
        {
            if (myCalculatedData.Count > BATCH_SIZE)
            {
                FlushBuildData(lifetime);
            }
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