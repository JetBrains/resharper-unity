using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Application.Progress;
using JetBrains.Collections.Synchronized;
using JetBrains.DocumentManagers.impl;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Caches
{
    [SolutionComponent]
    public class DeferredHelperCache : ICache
    {
        private readonly IPersistentIndexManager myPersistentIndexManager;
        private readonly IEnumerable<IDeferredCache> myCaches;
        private readonly SynchronizedSet<IPsiSourceFile> myActualFiles = new SynchronizedSet<IPsiSourceFile>();
        public readonly SynchronizedSet<IPsiSourceFile> FilesToDrop = new SynchronizedSet<IPsiSourceFile>();
        public readonly SynchronizedSet<IPsiSourceFile> FilesToProcess = new SynchronizedSet<IPsiSourceFile>();
        
        protected DeferredHelperCache(Lifetime lifetime, IPersistentIndexManager persistentIndexManager, IEnumerable<IDeferredCache> caches)
        {
            myPersistentIndexManager = persistentIndexManager;
            myCaches = caches;
        }
        
        public void MarkAsDirty(IPsiSourceFile sourceFile)
        {
        }

        public object Load(IProgressIndicator progress, bool enablePersistence)
        {
            return null;
        }

        public void MergeLoaded(object data)
        {
        }
        
        public void Save(IProgressIndicator progress, bool enablePersistence)
        {
        }

        public bool UpToDate(IPsiSourceFile sourceFile)
        {
            foreach (var cache in myCaches)
            {
                if (!cache.UpToDate(sourceFile))
                    return false;
            }

            return true;
        }

        public object Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            myActualFiles.Add(sourceFile);
            FilesToProcess.Add(sourceFile);
            return true;
        }

        public void Merge(IPsiSourceFile sourceFile, object builtPart)
        {
        }

        public void Drop(IPsiSourceFile sourceFile)
        {
            myActualFiles.Remove(sourceFile);
            FilesToProcess.Remove(sourceFile);
            FilesToDrop.Add(sourceFile);
        }

        public void OnPsiChange(ITreeNode elementContainingChanges, PsiChangedElementType type)
        {
        }

        public void OnDocumentChange(IPsiSourceFile sourceFile, ProjectFileDocumentCopyChange change)
        {
            throw new System.NotImplementedException();
        }

        public void SyncUpdate(bool underTransaction)
        {
        }

        public void Dump(TextWriter writer, IPsiSourceFile sourceFile)
        {
            var t = new int[5][];
            writer.Write("TODO Dump");
        }

        
        public bool HasDirtyFiles => true;//!myDirtyFiles.IsEmpty();
    }
}