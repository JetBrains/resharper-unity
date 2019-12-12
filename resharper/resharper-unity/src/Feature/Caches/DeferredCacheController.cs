using System.Collections.Generic;
using JetBrains.Application.changes;
using JetBrains.Application.PersistentMap;
using JetBrains.Collections.Synchronized;
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
        private readonly DaemonThread myDaemonThread;
        private readonly DeferredHelperCache myDeferredHelperCache;
        private readonly IEnumerable<IDeferredCache> myDeferredCaches;
        
        public DeferredCacheController(IPersistentIndexManager persistentIndexManager, DaemonThread daemonThread,
            DeferredHelperCache deferredHelperCache, IEnumerable<IDeferredCache> deferredCaches)
        {
            myDaemonThread = daemonThread;
            myDeferredHelperCache = deferredHelperCache;
            myDeferredCaches = deferredCaches;
        }
        public void GetTasks(Lifetime lifetime)
        {
            // TODO: remove task
            foreach (var psiSourceFile in myDeferredHelperCache.FilesToDrop)
            {
                foreach (var cache in myDeferredCaches)
                {
                    cache.Drop(psiSourceFile);
                }
                
                if (lifetime.IsAlive)
                    return;

            }
            myDeferredHelperCache.FilesToDrop.Clear();


            foreach (var psiSourceFile in myDeferredHelperCache.FilesToProcess.ToArray())
            {
                foreach (var cache in myDeferredCaches)
                {
                    cache.Merge(psiSourceFile, cache.Build(lifetime, psiSourceFile));
                    if (!lifetime.IsAlive)
                        return;
                }
                myDeferredHelperCache.FilesToProcess.Remove(psiSourceFile);
            }
            
        }
    }
}