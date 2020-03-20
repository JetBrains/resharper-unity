using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;
using JetBrains.Util.Caches;
using JetBrains.Application.Settings.Extentions;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Stripped;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches
{
    [SolutionComponent]
    public class PrefabImportCache
    {
        private readonly MetaFileGuidCache myMetaFileGuidCache;
        private readonly IShellLocks myShellLocks;
        private readonly OneToSetMap<string, string> myDependencies = new OneToSetMap<string, string>();
        private readonly DirectMappedCache<string, IDictionary<ulong, IHierarchyElement>> myCache = new DirectMappedCache<string, IDictionary<ulong, IHierarchyElement>>(100);
        private readonly UnityExternalFilesPsiModule myUnityExternalFilesPsiModule;
        private readonly IProperty<bool> myCacheEnabled;
        public PrefabImportCache(Lifetime lifetime, ISolution solution, ISettingsStore store, MetaFileGuidCache metaFileGuidCache, UnityExternalFilesModuleFactory unityExternalFilesModuleFactory, IShellLocks shellLocks)
        {
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
            
            var boundSettingsStoreLive = store.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()));
            myCacheEnabled = boundSettingsStoreLive.GetValueProperty(lifetime, (UnitySettings key) => key.IsPrefabCacheEnabled);
        }

        public void Add(IPsiSourceFile sourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement)
        {
            Remove(sourceFile, assetDocumentHierarchyElement);
        }

        public void Remove(IPsiSourceFile sourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement)
        {
            myShellLocks.AssertWriteAccessAllowed();
            var guid = myMetaFileGuidCache.GetAssetGuid(sourceFile);
            if (guid == null) // we have already clear content due to advice on GuidChanged in consructor
                return;
            
            var visited = new HashSet<string>();
            foreach (var deps in myDependencies.GetValuesSafe(guid))
            {
                InvalidateImportCache(deps, visited);
            }
            
            InvalidateImportCache(guid, visited);
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
        
        public IDictionary<ulong, IHierarchyElement> GetImportedElementsFor(UnityInterningCache interningCache, string ownerGuid,
            AssetDocumentHierarchyElement assetDocumentHierarchyElement)
        {
            myShellLocks.AssertReadAccessAllowed();
            if (!myCache.TryGetFromCache(ownerGuid, out var result))
            {
                lock (myLockObject)
                {
                    if (myCache.TryGetFromCache(ownerGuid, out result))
                        return result;

                    result = DoImport(interningCache, ownerGuid, assetDocumentHierarchyElement, new HashSet<string>());
                    StoreResult(ownerGuid, result);
                }
            }

            return result;
        }

        [NotNull]
        private IDictionary<ulong, IHierarchyElement> DoImport(UnityInterningCache interningCache, string ownerGuid, AssetDocumentHierarchyElement assetDocumentHierarchyElement, HashSet<string> visitedGuid)
        {
            var result = new Dictionary<ulong, IHierarchyElement>();
            foreach (var prefabInstanceHierarchy in assetDocumentHierarchyElement.GetPrefabInstanceHierarchies())
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
                        myDependencies.Add(guid, ownerGuid);
                        visitedGuid.Add(guid);
                        importedElements = DoImport(interningCache, guid, prefabHierarchy, visitedGuid);
                        StoreResult(guid, importedElements);
                    }
                    else
                    {
                        importedElements = EmptyDictionary<ulong, IHierarchyElement>.Instance;
                    }
                }

                foreach (var element in prefabHierarchy.Elements())
                {
                    if (element is IStrippedHierarchyElement)
                        continue;

                    if (element is IPrefabInstanceHierarchy)
                        continue;

                    var imported = element.Import(interningCache, prefabInstanceHierarchy);
                    if (imported == null)
                        continue;
                    result[imported.GetLocation(interningCache).LocalDocumentAnchor] = imported;
                }

                foreach (var element in importedElements.Values)
                {
                    Assertion.Assert(!(element is IStrippedHierarchyElement), "element should be imported");
                    Assertion.Assert(!(element is IPrefabInstanceHierarchy), "prefab should be imported");

                    var imported = element.Import(interningCache, prefabInstanceHierarchy);
                    if (imported == null)
                        continue;
                    
                    result[imported.GetLocation(interningCache).LocalDocumentAnchor] = imported;
                }
            }

            foreach (var value in result.Values)
            {
                var transform = value as ImportedTransformHierarchy;
                var reference = transform?.GetOwner(interningCache);
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
            if (!myCacheEnabled.Value)
                return;
            
            Assertion.Assert(hierarchyElements !=  null, "hierarchyElements !=  null");
            Assertion.Assert(!myCache.ContainsKeyInCache(ownerGuid), "!myCache.ContainsKey(ownerGuid)");
            myCache.AddToCache(ownerGuid, hierarchyElements);
        }

        public void Invalidate()
        {
            myDependencies.Clear();
            myCache.Clear();
        }
    }
}