#nullable enable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Common.Psi.Caches
{
    public abstract class SimplePsiSourceFileCacheWithLocalCache<TPersistent, TLocal> : PsiSourceFileCacheWithLocalCache<TPersistent> where TPersistent : class
    {
        private readonly Dictionary<IPsiSourceFile, TLocal> myLocalCache;

        public IReadOnlyCollection<TLocal> LocalCacheValues
        {
            get
            {
                Locks.AssertReadAccessAllowed();
                return myLocalCache.Values;
            }
        }

        protected SimplePsiSourceFileCacheWithLocalCache(Lifetime lifetime, IShellLocks locks, IPersistentIndexManager persistentIndexManager, IUnsafeMarshaller<TPersistent> valueMarshaller, string cacheChangedEvent) : base(lifetime, locks, persistentIndexManager, valueMarshaller, cacheChangedEvent)
        {
            myLocalCache = new(persistentIndexManager.PsiSourceFilePersistentEqualityComparer);
        }

        public bool TryGetLocalCacheValue(IPsiSourceFile sourceFile, [MaybeNullWhen(false)] out TLocal local)
        {
            Locks.AssertReadAccessAllowed();
            return myLocalCache.TryGetValue(sourceFile, out local);
        }
        
        protected abstract TLocal BuildLocal(IPsiSourceFile sourceFile, TPersistent persistent);

        protected virtual void OnLocalRemoved(IPsiSourceFile sourceFile, TLocal local)
        {
        }
        
        protected sealed override bool RemoveFromLocalCache(IPsiSourceFile sourceFile)
        {
            if (!myLocalCache.TryGetValue(sourceFile, out var local))
                return false;
            
            myLocalCache.Remove(sourceFile);
            OnLocalRemoved(sourceFile, local);
            return true;
        }

        protected sealed override bool AddToLocalCache(IPsiSourceFile sourceFile, TPersistent newPart)
        {
            if (BuildLocal(sourceFile, newPart) is not { } newLocal)
                return false;
            
            myLocalCache.Add(sourceFile, newLocal);
            return true;
        }
    }
}