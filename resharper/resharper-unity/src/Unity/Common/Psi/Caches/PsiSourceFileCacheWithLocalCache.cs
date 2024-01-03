#nullable enable
using System;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.Threading;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Common.Psi.Caches
{
    public abstract class PsiSourceFileCacheWithLocalCache<T> : SimpleICache<T> where T : class
    {
        private readonly GroupingEvent myCacheUpdatedGroupingEvent;
        
        [PublicAPI] public ISimpleSignal CacheUpdated => myCacheUpdatedGroupingEvent.Outgoing;
        
        protected PsiSourceFileCacheWithLocalCache(Lifetime lifetime, IShellLocks locks, IPersistentIndexManager persistentIndexManager, IUnsafeMarshaller<T> valueMarshaller, string cacheChangedEvent) : base(lifetime, locks, persistentIndexManager, valueMarshaller)
        {
            myCacheUpdatedGroupingEvent = Locks.CreateGroupingEvent(lifetime, cacheChangedEvent, TimeSpan.FromMilliseconds(500));
        }
        
        public sealed override void Drop(IPsiSourceFile sourceFile)
        {
            var removed = RemoveFromLocalCache(sourceFile);
            base.Drop(sourceFile);
            if (removed)
                myCacheUpdatedGroupingEvent.FireIncoming();
        }

        public sealed override void Merge(IPsiSourceFile sourceFile, object? builtPart)
        {
            var newPart = builtPart as T;
            var updated = RemoveFromLocalCache(sourceFile);
            if (newPart != null)
                updated |= AddToLocalCache(sourceFile, newPart);

            base.Merge(sourceFile, builtPart);
            if (updated)
                myCacheUpdatedGroupingEvent.FireIncoming();
        }

        public override void MergeLoaded(object data)
        {
            foreach (var (sourceFile, cacheItem) in Map)
                AddToLocalCache(sourceFile, cacheItem);
            base.MergeLoaded(data);
            myCacheUpdatedGroupingEvent.FireIncoming();
        }

        protected abstract bool RemoveFromLocalCache(IPsiSourceFile sourceFile);

        protected abstract bool AddToLocalCache(IPsiSourceFile sourceFile, T newPart);
    }
}