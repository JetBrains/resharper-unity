using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
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
        
        private readonly IShellLocks myShellLocks;
        private readonly DeferredCachesLocks myDeferredCachesLocks;
        private readonly DeferredHelperCache myDeferredHelperCache;
        private readonly IEnumerable<IDeferredCache> myDeferredCaches;

        private readonly Dictionary<IPsiSourceFile, Dictionary<IDeferredCache, object>> myPartlyCalculatedData = new Dictionary<IPsiSourceFile, Dictionary<IDeferredCache, object>>();
        private readonly Dictionary<IPsiSourceFile, Dictionary<IDeferredCache, object>> myCalculatedData = new Dictionary<IPsiSourceFile, Dictionary<IDeferredCache, object>>();
        
        public DeferredCacheController(IPersistentIndexManager persistentIndexManager, IShellLocks shellLocks, DeferredCachesLocks deferredCachesLocks,
            DeferredHelperCache deferredHelperCache, IEnumerable<IDeferredCache> deferredCaches)
        {
            myShellLocks = shellLocks;
            myDeferredCachesLocks = deferredCachesLocks;
            myDeferredHelperCache = deferredHelperCache;
            myDeferredCaches = deferredCaches;
        }
        public Action CreateTask(Lifetime lifetime)
        {
            return () =>
            {
                using (TryReadLockCookie.Create(NullProgressIndicator.Instance, myShellLocks, () => !lifetime.IsAlive))
                {
                    // First of all, drop data for out-of-date files under DeferredCachesWriteLock
                    foreach (var psiSourceFile in new LocalList<IPsiSourceFile>(myDeferredHelperCache.FilesToDrop))
                    {
                        myPartlyCalculatedData.Remove(psiSourceFile);
                        myCalculatedData.Remove(psiSourceFile);
                        
                        myDeferredCachesLocks.ExecuteUnderWriteLock(() =>
                        {
                            // Drop operation should be fast, do not interrupt here
                            foreach (var cache in myDeferredCaches)
                            {
                                cache.Drop(psiSourceFile);
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

                            
                                cacheToData[cache] = cache.Build(lifetime, psiSourceFile);
                                CheckForInterrupt(lifetime);
                            }
                        });

                        myDeferredHelperCache.FilesToProcess.Remove(psiSourceFile);

                        Assertion.Assert(!myCalculatedData.ContainsKey(psiSourceFile),
                            "!myCalculatedData.ContainsKey(psiSourceFile)");
                        myCalculatedData[psiSourceFile] = myPartlyCalculatedData[psiSourceFile];
                        myPartlyCalculatedData.Remove(psiSourceFile);

                        FlushBuildDataIfNeed(lifetime);
                    }

                    FlushBuildData(lifetime);
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
                var cacheToData = myCalculatedData[sourceFile];
                myDeferredCachesLocks.ExecuteUnderWriteLock(() =>
                {
                    foreach (var (cache, data) in cacheToData)
                    {
                        cache.Merge(sourceFile, data);
                    }
                });

                myCalculatedData.Remove(sourceFile);

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