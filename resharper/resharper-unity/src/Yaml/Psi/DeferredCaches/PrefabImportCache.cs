using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;
using JetBrains.Util.Caches;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches
{
    [SolutionComponent]
    public class PrefabImportCache
    {
        private readonly DeferredCachesLocks myDeferredCachesLocks;
        private readonly MetaFileGuidCache myMetaFileGuidCache;
        private readonly IShellLocks myShellLocks;
        private readonly OneToSetMap<string, string> myDependencies = new OneToSetMap<string, string>();
        private readonly DirectMappedCache<string, IDictionary<ulong, IHierarchyElement>> myCache = new DirectMappedCache<string, IDictionary<ulong, IHierarchyElement>>(100);
        private readonly UnityExternalFilesPsiModule myUnityExternalFilesPsiModule;
        
        public PrefabImportCache(Lifetime lifetime, DeferredCachesLocks deferredCachesLocks, MetaFileGuidCache metaFileGuidCache, UnityExternalFilesModuleFactory unityExternalFilesModuleFactory, IShellLocks shellLocks)
        {
            myDeferredCachesLocks = deferredCachesLocks;
            myMetaFileGuidCache = metaFileGuidCache;
            myShellLocks = shellLocks;
            metaFileGuidCache.GuidChanged.Advise(lifetime, e =>
            {
                myShellLocks.AssertWriteAccessAllowed();
                var set = new HashSet<string>();
                if (e.oldGuid != null)
                    InvalidateImportCache(e.oldGuid, set);

                if (e.newGuid != null)
                    InvalidateImportCache(e.newGuid, set);
            });

            myUnityExternalFilesPsiModule = unityExternalFilesModuleFactory.PsiModule;
        }

        public void Add(IPsiSourceFile sourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement)
        {
            myDeferredCachesLocks.AssertWriteAccessAllowed();
            var guid = myMetaFileGuidCache.GetAssetGuid(sourceFile);
            foreach (var prefabInstanceHierarchy in assetDocumentHierarchyElement.PrefabInstanceHierarchies)
            {
                myDependencies.Add(prefabInstanceHierarchy.SourcePrefabGuid, guid);
            }
        }

        public void Remove(IPsiSourceFile sourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement)
        {
            myDeferredCachesLocks.AssertWriteAccessAllowed();
            var guid = myMetaFileGuidCache.GetAssetGuid(sourceFile);
            if (guid == null) // we have already clear content due to advice on GuidChanged in consructor
                return;
            
            var visited = new HashSet<string>();
            foreach (var deps in myDependencies.GetValuesSafe(guid))
            {
                InvalidateImportCache(deps, visited);
            }
            
            foreach (var prefabInstanceHierarchy in assetDocumentHierarchyElement.PrefabInstanceHierarchies)
            {
                myDependencies.Remove(prefabInstanceHierarchy.SourcePrefabGuid, guid);
            }
        }

        private void InvalidateImportCache(string deps, HashSet<string> visited)
        {
            myShellLocks.IsWriteAccessAllowed();
            visited.Add(deps);
            myCache.RemoveFromCache(deps);
            foreach (var d in myDependencies.GetValuesSafe(deps))
            {
                if (!visited.Contains(d))
                    InvalidateImportCache(d, visited);
            }
        }

        private readonly object myLockObject = new object();
        
        public IDictionary<ulong, IHierarchyElement> GetImportedElementsFor(string ownerGuid,
            AssetDocumentHierarchyElement assetDocumentHierarchyElement)
        {
            myDeferredCachesLocks.AssertReadAccessAllowed();
            if (!myCache.TryGetFromCache(ownerGuid, out var result))
            {
                lock (myLockObject)
                {
                    if (myCache.TryGetFromCache(ownerGuid, out result))
                        return result;

                    result = DoImport(assetDocumentHierarchyElement, new HashSet<string>());
                    StoreResult(ownerGuid, result);
                }
            }

            return result;
        }

        [NotNull]
        private IDictionary<ulong, IHierarchyElement> DoImport(AssetDocumentHierarchyElement assetDocumentHierarchyElement, HashSet<string> visitedGuid)
        {
            var result = new Dictionary<ulong, IHierarchyElement>();
            foreach (var prefabInstanceHierarchy in assetDocumentHierarchyElement.PrefabInstanceHierarchies)
            {
                var guid = prefabInstanceHierarchy.SourcePrefabGuid;
                var sourceFilePath = myMetaFileGuidCache.GetAssetFilePathsFromGuid(guid).FirstOrDefault();
                if (sourceFilePath == null)
                    continue;
                if (!myUnityExternalFilesPsiModule.TryGetFileByPath(sourceFilePath, out var sourceFile))
                    continue;
                
                var prefabHierarchy = assetDocumentHierarchyElement.AssetDocumentHierarchyElementContainer.GetAssetHierarchyFor(sourceFile);
                if (prefabHierarchy == null)
                    continue;

                if (!myCache.TryGetFromCache(guid, out var importedElements))
                {
                    if (!visitedGuid.Contains(guid)) // invalid assets with cycles in prefab imports
                    {
                        visitedGuid.Add(guid);
                        importedElements = DoImport(prefabHierarchy, visitedGuid);
                        StoreResult(guid, importedElements);
                    }
                    else
                    {
                        importedElements = EmptyDictionary<ulong, IHierarchyElement>.Instance;
                    }
                }

                foreach (var element in prefabHierarchy.Elements)
                {
                    if (element.IsStripped)
                        continue;

                    if (element is IPrefabInstanceHierarchy)
                        continue;

                    var imported = element.Import(prefabInstanceHierarchy);
                    if (imported == null)
                        continue;
                    result[imported.Location.LocalDocumentAnchor] = imported;
                }

                foreach (var element in importedElements.Values)
                {
                    Assertion.Assert(!element.IsStripped, "element should be imported");
                    Assertion.Assert(!(element is IPrefabInstanceHierarchy), "prefab should be imported");

                    var imported = element.Import(prefabInstanceHierarchy);
                    if (imported == null)
                        continue;
                    
                    result[imported.Location.LocalDocumentAnchor] = imported;
                }
            }

            foreach (var value in result.Values)
            {
                var transform = value as ImportedTransformHierarchy;
                var reference = transform?.GameObjectReference;
                if (reference == null)
                    continue;

                var importedGameObject = result.GetValueSafe(reference.LocalDocumentAnchor) as ImportedGameObjectHierarchy;
                if (importedGameObject == null)
                    continue;

                importedGameObject.TransformHierarchy = transform;

            }

            return result;
        }

        
        private void StoreResult(string ownerGuid, [NotNull]IDictionary<ulong, IHierarchyElement> hierarchyElements)
        {
            Assertion.Assert(hierarchyElements !=  null, "hierarchyElements !=  null");
            Assertion.Assert(!myCache.ContainsKeyInCache(ownerGuid), "!myCache.ContainsKey(ownerGuid)");
            myCache.AddToCache(ownerGuid, hierarchyElements);
        }
    }
}