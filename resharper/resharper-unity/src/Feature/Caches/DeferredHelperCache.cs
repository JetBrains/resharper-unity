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
        public readonly SynchronizedSet<IPsiSourceFile> FilesToDrop = new SynchronizedSet<IPsiSourceFile>();
        public readonly SynchronizedSet<IPsiSourceFile> FilesToProcess = new SynchronizedSet<IPsiSourceFile>();
        
        public DeferredHelperCache(Lifetime lifetime, IPersistentIndexManager persistentIndexManager, IEnumerable<IDeferredCache> caches)
        {
            myPersistentIndexManager = persistentIndexManager;
            myCaches = caches;
        }
        
        public void MarkAsDirty(IPsiSourceFile sourceFile)
        
        {
            FilesToProcess.Add(sourceFile);
        }

        public object Load(IProgressIndicator progress, bool enablePersistence)
        {
            foreach (var cache in myCaches)
            {
                cache.Load();
            }

            return null;
        }

        public void MergeLoaded(object data)
        {
            foreach (var cache in myCaches)
            {
                cache.MergeLoadedData();
            }
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
            FilesToProcess.Add(sourceFile);
            return true;
        }

        public void Merge(IPsiSourceFile sourceFile, object builtPart)
        {
            FilesToProcess.Add(sourceFile);
            // TODO
            // here add FilesToProcess
            // here add to FilesToDrop
            // Add assertion in contoller that each file to process is valid
        }

        public void Drop(IPsiSourceFile sourceFile)
        {
            FilesToProcess.Remove(sourceFile);
            FilesToDrop.Add(sourceFile);
        }

        public void OnPsiChange(ITreeNode elementContainingChanges, PsiChangedElementType type)
        {
            var sourceFile = elementContainingChanges?.GetSourceFile();
            if (sourceFile != null)
            {
                FilesToProcess.Add(sourceFile);
            }
        }

        public void OnDocumentChange(IPsiSourceFile sourceFile, ProjectFileDocumentCopyChange change)
        {
            FilesToProcess.Add(sourceFile);
        }

        public void SyncUpdate(bool underTransaction)
        {
        }

        public void Dump(TextWriter writer, IPsiSourceFile sourceFile)
        {
        }

        
        public bool HasDirtyFiles => false;//!myDirtyFiles.IsEmpty();
    }
}