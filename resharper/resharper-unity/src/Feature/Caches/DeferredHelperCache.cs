using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Application.Progress;
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
    public class DeferredHelperCache : SimpleICache<bool>
    {
        private readonly IPersistentIndexManager myPersistentIndexManager;
        private readonly IEnumerable<IDeferredCache> myCaches;
        public readonly SynchronizedSet<IPsiSourceFile> FilesToDrop = new SynchronizedSet<IPsiSourceFile>();
        public readonly SynchronizedSet<IPsiSourceFile> FilesToProcess = new SynchronizedSet<IPsiSourceFile>();
        
        public DeferredHelperCache(Lifetime lifetime, IPersistentIndexManager persistentIndexManager,
            IEnumerable<IDeferredCache> caches) : base(lifetime, persistentIndexManager, UnsafeMarshallers.BooleanMarshaller)
        {
            myPersistentIndexManager = persistentIndexManager;
            myCaches = caches;
        }
        
        public override object Load(IProgressIndicator progress, bool enablePersistence)
        {
            base.Load(progress, enablePersistence);
            
            foreach (var cache in myCaches)
            {
                cache.Load();
            }

            return null;
        }

        public override void MergeLoaded(object data)
        {
            base.MergeLoaded(data);
            
            foreach (var cache in myCaches)
            {
                cache.MergeLoadedData();
            }
        }
        
        public override bool UpToDate(IPsiSourceFile sourceFile)
        {
            foreach (var cache in myCaches)
            {
                if (!cache.UpToDate(sourceFile))
                    return false;
            }

            return true;
        }

        public override object Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            return true;
        }

        public override void Merge(IPsiSourceFile sourceFile, object builtPart)
        {
            if (Map.ContainsKey(sourceFile))
                Drop(sourceFile);
                
            AddToProcess(sourceFile);
            FilesToDrop.Remove(sourceFile);
            base.Merge(sourceFile, builtPart);
        }

        public override void Drop(IPsiSourceFile sourceFile)
        {
            DropFromProcess(sourceFile, true);
            FilesToDrop.Add(sourceFile);
            base.Drop(sourceFile);
        }

        public override void OnDocumentChange(IPsiSourceFile sourceFile, ProjectFileDocumentCopyChange change)
        {
            // TODO : temp solution
            if (sourceFile is UnityYamlExternalPsiSourceFile unityYamlExternalPsiSourceFile)
            {
                unityYamlExternalPsiSourceFile.MarkDocumentModified();
            }
            
            base.OnDocumentChange(sourceFile, change);
        }
        
        private void AddToProcess(IPsiSourceFile sourceFile)
        {
            bool isApplicable = myCaches.Any(t => t.IsApplicable(sourceFile));
            if (isApplicable)
            {
                FilesToProcess.Add(sourceFile);
                AfterAddToProcess.Fire(sourceFile);
            }
        }

        public void DropFromProcess(IPsiSourceFile sourceFile, bool isDropped)
        {
            bool isApplicable = myCaches.Any(t => t.IsApplicable(sourceFile));
            if (isApplicable)
            {
                FilesToProcess.Remove(sourceFile);
                AfterRemoveFromProcess.Fire((sourceFile, isDropped));
            }
        }
        
        public Signal<IPsiSourceFile> AfterAddToProcess = new Signal<IPsiSourceFile>();
        public Signal<(IPsiSourceFile file, bool isDropped)> AfterRemoveFromProcess = new Signal<(IPsiSourceFile, bool)>();
    }
}