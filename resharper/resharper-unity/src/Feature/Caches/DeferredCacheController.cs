using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
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

        private readonly Lifetime myLifetime;
        private readonly SolutionAnalysisConfiguration mySolutionAnalysisConfiguration;
        private readonly IShellLocks myShellLocks;
        private readonly DeferredCachesLocks myDeferredCachesLocks;
        private readonly DeferredHelperCache myDeferredHelperCache;
        private readonly IEnumerable<IDeferredCache> myDeferredCaches;
        private readonly ILogger myLogger;

        private readonly Dictionary<IPsiSourceFile, Dictionary<IDeferredCache, object>> myPartlyCalculatedData = new Dictionary<IPsiSourceFile, Dictionary<IDeferredCache, object>>();
        private readonly Dictionary<IPsiSourceFile, Dictionary<IDeferredCache, object>> myCalculatedData = new Dictionary<IPsiSourceFile, Dictionary<IDeferredCache, object>>();
        private LifetimeDefinition mySwaPauseLifetimeDef;
        
        
        public DeferredCacheController(Lifetime lifetime, IPersistentIndexManager persistentIndexManager, SolutionAnalysisConfiguration solutionAnalysisConfiguration, IShellLocks shellLocks, DeferredCachesLocks deferredCachesLocks,
            DeferredHelperCache deferredHelperCache, IEnumerable<IDeferredCache> deferredCaches, ILogger logger)
        {
            myLifetime = lifetime;
            mySolutionAnalysisConfiguration = solutionAnalysisConfiguration;
            myShellLocks = shellLocks;
            myDeferredCachesLocks = deferredCachesLocks;
            myDeferredHelperCache = deferredHelperCache;
            myDeferredCaches = deferredCaches;
            myLogger = logger;
        }

        public Action CreateTask(Lifetime lifetime)
        {
            return () =>
            {
                using (TryReadLockCookie.Create(NullProgressIndicator.Instance, myShellLocks, () => !lifetime.IsAlive))
                {
                    if (mySwaPauseLifetimeDef == null)
                    {
                        myLogger.Info("Start processing files in deferred caches");
                        mySwaPauseLifetimeDef = myLifetime.CreateNested();
                        mySolutionAnalysisConfiguration.Pause(mySwaPauseLifetimeDef.Lifetime, "Deferred index is calculated");
                    }
                    
                    // First of all, drop data for out-of-date files under DeferredCachesWriteLock
                    foreach (var psiSourceFile in new LocalList<IPsiSourceFile>(myDeferredHelperCache.FilesToDrop))
                    {
                        myLogger.Info("Drop {0}", psiSourceFile.GetPersistentIdForLogging());
                        myPartlyCalculatedData.Remove(psiSourceFile);
                        myCalculatedData.Remove(psiSourceFile);
                        
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
                        myCalculatedData.Remove(psiSourceFile);
                    }
                    
                    // Possibly, there was interruption in previous flush, prioritize data flushing
                    FlushBuildDataIfNeed(lifetime);

                    foreach (var psiSourceFile in GetFilesToProcess())
                    {
                        myLogger.Info("Build started {0}", psiSourceFile.GetPersistentIdForLogging());
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
                                catch (Exception e)
                                {
                                    myLogger.Error(e, "An error occurred during build cache {0}", cache.GetType().Name);
                                }

                                CheckForInterrupt(lifetime);
                            }
                        });

                        myDeferredHelperCache.FilesToProcess.Remove(psiSourceFile);

                        Assertion.Assert(!myCalculatedData.ContainsKey(psiSourceFile),
                            "!myCalculatedData.ContainsKey(psiSourceFile)");
                        myCalculatedData[psiSourceFile] = myPartlyCalculatedData[psiSourceFile];
                        myPartlyCalculatedData.Remove(psiSourceFile);

                        FlushBuildDataIfNeed(lifetime);
                        myLogger.Info("Build finished {0}", psiSourceFile.GetPersistentIdForLogging());

                    }

                    FlushBuildData(lifetime);
                    if (mySwaPauseLifetimeDef.Lifetime.IsAlive)
                    {
                        myLogger.Info("Finish processing files in deferred caches");
                        mySwaPauseLifetimeDef.Terminate();
                    }
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
                myLogger.Info("Start merging for {0}", sourceFile);
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

                myCalculatedData.Remove(sourceFile);

                myLogger.Info("Finish merging for {0}", sourceFile);
                CheckForInterrupt(lifetime);
            }
        }

        private void FlushBuildDataIfNeed(Lifetime lifetime)
        {
            
            if (myCalculatedData.Count > BATCH_SIZE)
            {
                FlushBuildData(lifetime);
            }
        }
        
    }
}