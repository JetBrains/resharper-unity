using JetBrains.Application.PersistentMap;
using JetBrains.Application.Threading;
using JetBrains.DocumentManagers.impl;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Caches
{
    // Life cycle of cache is similar to SimpleICache
    // At startup: Load -> MergeLoaded
    // For new file : Build (RL) -> MergeData (WL)
    // For modified file: Build (RL)-> DropData (old, WL) -> MergeData (new, WL)
    // For dropped file: Drop (WL)
    // (!!!) The biggest difference is that Build, Drop, Merge are not executed at start up and will
    // be invoked when local daemon will finish the work, Drop and Merge uses DeferredCacheWriteLock instead of standard WriteLock (preference to user input)
    // DeferredCacheWriteLock could be used only under real WriteLock or under ReadLock at daemon thread(all psi committed) and only for
    // modification internal deferred cache data(e.g building index), no callbacks and so on
    // Data which is stored in Deferred cache should be accessed only under DeferredCacheReadLock
    public abstract class DeferredCacheBase<T> : IDeferredCache
    {
        private readonly IPersistentIndexManager myPersistentIndexManager;
        public IOptimizedPersistentSortedMap<IPsiSourceFile, T> Map { get; }
        private readonly IOptimizedPersistentSortedMap<IPsiSourceFile, long> myTimeStamps;
        protected DeferredCacheBase(Lifetime lifetime, IPersistentIndexManager persistentIndexManager, IUnsafeMarshaller<T> valueMarshaller)
        {
            myPersistentIndexManager = persistentIndexManager;
            Map = myPersistentIndexManager.GetPersistentMap(lifetime, PersistentId, valueMarshaller);
            myTimeStamps = myPersistentIndexManager.GetPersistentMap(lifetime, PersistentId + "Time", UnsafeMarshallers.LongMarshaller);
        }

        public virtual bool IsApplicable(IPsiSourceFile sourceFile) => true;

        public virtual bool UpToDate(IPsiSourceFile sourceFile)
        {
            if (!IsApplicable(sourceFile))
                return true;

            if (!myTimeStamps.TryGetValue(sourceFile, out var value))
                return false;
            
            return value == sourceFile.GetAggregatedTimestamp() && Map.ContainsKey(sourceFile);
        }

        public void Merge(IPsiSourceFile psiSourceFile, object build)
        {
            Drop(psiSourceFile);

            if (build != null)
            {
                MergeData(psiSourceFile, (T) build);
                Map[psiSourceFile] = (T) build;
                myTimeStamps[psiSourceFile] = psiSourceFile.GetAggregatedTimestamp();
            }
            else
            {
                Map.Remove(psiSourceFile);
            }
        }

        /// <summary>
        /// This method is executed under DeferredCachesWriteLock
        /// </summary>
        protected abstract void MergeData(IPsiSourceFile sourceFile, T build);

        /// <summary>
        /// This method is executed under standard ReadLock
        /// </summary>
        public abstract object Build(IPsiSourceFile psiSourceFile);

        /// <summary>
        /// This method is executed under DeferredCachesWriteLock
        /// </summary>
        public void Drop(IPsiSourceFile psiSourceFile)
        {
            if (Map.TryGetValue(psiSourceFile, out var data))
            {
                myTimeStamps.Remove(psiSourceFile);
                Map.Remove(psiSourceFile);
                DropData(psiSourceFile, data);
            }
        }

        public abstract void DropData(IPsiSourceFile sourceFile, T data);

        public virtual void Load()
        {
            Map.Clear(sf => sf == null  || !sf.IsValid());
        }

        public abstract void MergeLoadedData();
        public void Invalidate()
        {
            myPersistentIndexManager.Solution.GetComponent<IShellLocks>().AssertWriteAccessAllowed();
            InvalidateData();
            Map.Clear();
        }

        public virtual void OnDocumentChange(IPsiSourceFile sourceFile, ProjectFileDocumentCopyChange change)
        {
        }

        /// <summary>
        /// This method is executed under standard WriteLock
        /// </summary>
        public abstract void InvalidateData();

        public string PersistentId => GetType().Name;
    }
}