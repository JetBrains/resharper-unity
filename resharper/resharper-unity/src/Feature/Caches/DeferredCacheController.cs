using System;
using System.Collections.Generic;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Caches
{
    [SolutionComponent]
    public class DeferredCacheController
    {
        private const int BATCH_SIZE = 10;
        
        private readonly IShellLocks myShellLocks;
        private readonly DaemonThread myDaemonThread;
        private readonly DeferredHelperCache myDeferredHelperCache;
        private readonly IEnumerable<IDeferredCache> myDeferredCaches;

        private readonly Dictionary<IPsiSourceFile, Dictionary<IDeferredCache, object>> myPartlyCalculatedData = new Dictionary<IPsiSourceFile, Dictionary<IDeferredCache, object>>();
        private readonly Dictionary<IPsiSourceFile, Dictionary<IDeferredCache, object>> myCalculatedData = new Dictionary<IPsiSourceFile, Dictionary<IDeferredCache, object>>();
        
        public DeferredCacheController(IPersistentIndexManager persistentIndexManager, IShellLocks shellLocks, DaemonThread daemonThread,
            DeferredHelperCache deferredHelperCache, IEnumerable<IDeferredCache> deferredCaches)
        {
            myShellLocks = shellLocks;
            myDaemonThread = daemonThread;
            myDeferredHelperCache = deferredHelperCache;
            myDeferredCaches = deferredCaches;
        }
        public void GetTasks(Lifetime lifetime)
        {
            using (TryReadLockCookie.Create(NullProgressIndicator.Instance, myShellLocks, () => !lifetime.IsAlive))
            {
                foreach (var psiSourceFile in new LocalList<IPsiSourceFile>(myDeferredHelperCache.FilesToDrop))
                {
                    myPartlyCalculatedData.Remove(psiSourceFile);
                    myCalculatedData.Remove(psiSourceFile);
                    // TODO: enter write lock
                    foreach (var cache in myDeferredCaches)
                    {
                        cache.Drop(psiSourceFile);
                    }
                    
                    // DeferredCacheController is unique reader for data in DeferredHelperCache
                    // Clearing list is safe operation here due to ReadLock
                    myDeferredHelperCache.FilesToDrop.Remove(psiSourceFile);
                    CheckForInterrupt(lifetime);
                }
                myDeferredHelperCache.FilesToDrop.Clear();

                FlushBuildDataIfNeed(lifetime);
                
                foreach (var psiSourceFile in GetFilesToProcess())
                {
                    if (!myPartlyCalculatedData.TryGetValue(psiSourceFile, out var cacheToData))
                    {
                        cacheToData = new Dictionary<IDeferredCache, object>();
                        myPartlyCalculatedData[psiSourceFile] = cacheToData;
                    }
                    
                    foreach (var cache in myDeferredCaches)
                    {
                        if (cacheToData.ContainsKey(cache))
                            continue;

                        cacheToData[cache] = cache.Build(lifetime, psiSourceFile);
                        CheckForInterrupt(lifetime);
                    }
                    myDeferredHelperCache.FilesToProcess.Remove(psiSourceFile);
                    
                    Assertion.Assert(!myCalculatedData.ContainsKey(psiSourceFile), "!myCalculatedData.ContainsKey(psiSourceFile)");
                    myCalculatedData[psiSourceFile] = myPartlyCalculatedData[psiSourceFile];
                    myPartlyCalculatedData.Remove(psiSourceFile);

                    FlushBuildDataIfNeed(lifetime);
                }

                FlushBuildData(lifetime);
            }
        }

        private void CheckForInterrupt(Lifetime lifetime)
        {
            if (!lifetime.IsAlive)
                throw new OperationCanceledException();
        }

        private IEnumerable<IPsiSourceFile> GetFilesToProcess()
        {
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
                foreach (var (cache, data) in cacheToData)
                {
                    cache.Merge(sourceFile, data);
                }

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