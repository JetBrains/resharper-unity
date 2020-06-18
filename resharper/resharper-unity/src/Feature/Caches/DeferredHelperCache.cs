using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.Collections.Synchronized;
using JetBrains.Collections.Viewable;
using JetBrains.DocumentManagers.impl;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Caches
{
    [SolutionComponent]
    public class DeferredHelperCache : IPsiSourceFileCache
    {
        private readonly IShellLocks myShellLocks;
        private readonly IEnumerable<IDeferredCache> myCaches;
        public readonly SynchronizedSet<IPsiSourceFile> FilesToDrop = new SynchronizedSet<IPsiSourceFile>();
        public readonly SynchronizedSet<IPsiSourceFile> FilesToProcess = new SynchronizedSet<IPsiSourceFile>();
        
        public DeferredHelperCache(Lifetime lifetime, IShellLocks shellLocks, IEnumerable<IDeferredCache> caches)
        {
            myShellLocks = shellLocks;
            myCaches = caches;
        }
        
        public void MarkAsDirty(IPsiSourceFile sourceFile)
        {
            AddToProcess(sourceFile);
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
            return true;
        }

        public void Merge(IPsiSourceFile sourceFile, object builtPart)
        {
            AddToProcess(sourceFile);
        }

        public void Drop(IPsiSourceFile sourceFile)
        {
            DropFromProcess(sourceFile);
            
            bool isApplicable = myCaches.Any(t => t.IsApplicable(sourceFile));
            if (isApplicable)
                FilesToDrop.Add(sourceFile);
        }

        public void OnPsiChange(ITreeNode elementContainingChanges, PsiChangedElementType type)
        {
            var sourceFile = elementContainingChanges?.GetSourceFile();
            if (sourceFile != null)
            {
                AddToProcess(sourceFile);
            }
        }

        public void OnDocumentChange(IPsiSourceFile sourceFile, ProjectFileDocumentCopyChange change)
        {
            // TODO : temp solution
            if (sourceFile is UnityYamlExternalPsiSourceFile unityYamlExternalPsiSourceFile)
            {
                unityYamlExternalPsiSourceFile.MarkDocumentModified();
            }
            
            AddToProcess(sourceFile);
        }

        public void SyncUpdate(bool underTransaction)
        {
        }

        public void Dump(TextWriter writer, IPsiSourceFile sourceFile)
        {
        }

        private void AddToProcess(IPsiSourceFile sourceFile)
        {
            myShellLocks.Dispatcher.AssertAccess();
            bool isApplicable = myCaches.Any(t => t.IsApplicable(sourceFile));
            if (isApplicable)
            {
                FilesToProcess.Add(sourceFile);
                AfterAddToProcess.Fire(sourceFile);
            }
        }

        public void DropFromProcess(IPsiSourceFile sourceFile)
        {
            myShellLocks.Dispatcher.AssertAccess();
            bool isApplicable = myCaches.Any(t => t.IsApplicable(sourceFile));
            if (isApplicable)
            {
                FilesToProcess.Remove(sourceFile);
                AfterRemoveFromProcess.Fire(sourceFile);
            }
        }
        
        public bool HasDirtyFiles => false;//!myDirtyFiles.IsEmpty();
        
        public Signal<IPsiSourceFile> AfterAddToProcess = new Signal<IPsiSourceFile>();
        public Signal<IPsiSourceFile> AfterRemoveFromProcess = new Signal<IPsiSourceFile>();
    }
}