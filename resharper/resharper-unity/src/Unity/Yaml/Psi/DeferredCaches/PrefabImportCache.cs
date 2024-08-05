using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;
using JetBrains.Util.Caches;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Stripped;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches
{
    [SolutionComponent(InstantiationEx.LegacyDefault)]
    public class PrefabImportCache
    {
        private readonly MetaFileGuidCache myMetaFileGuidCache;
        private readonly IShellLocks myShellLocks;
        private readonly OneToSetMap<Guid, Guid> myDependencies = new OneToSetMap<Guid, Guid>();
        private readonly DirectMappedCache<Guid, IDictionary<long, IHierarchyElement>> myCache = new DirectMappedCache<Guid, IDictionary<long, IHierarchyElement>>(100);
        private readonly UnityExternalFilesPsiModule myUnityExternalFilesPsiModule;
        private readonly IProperty<bool> myCacheEnabled;

        public PrefabImportCache(Lifetime lifetime, ISolution solution,
                                 IApplicationWideContextBoundSettingStore settingStore,
                                 MetaFileGuidCache metaFileGuidCache,
                                 UnityExternalFilesModuleFactory unityExternalFilesModuleFactory,
                                 IShellLocks shellLocks)
        {
            myMetaFileGuidCache = metaFileGuidCache;
            myShellLocks = shellLocks;
            metaFileGuidCache.GuidChanged.Advise(lifetime, e =>
            {
                myShellLocks.AssertWriteAccessAllowed();
                var set = new HashSet<Guid>();
                if (e.oldGuid != null)
                    InvalidateImportCache(e.oldGuid.Value, set);

                if (e.newGuid != null)
                    InvalidateImportCache(e.newGuid.Value, set);
            });

            myUnityExternalFilesPsiModule = unityExternalFilesModuleFactory.PsiModule;

            myCacheEnabled = settingStore.BoundSettingsStore
                .GetValueProperty(lifetime, (UnitySettings key) => key.IsPrefabCacheEnabled);
        }

        public void OnHierarchyCreated(IPsiSourceFile sourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement)
        {
            // Invalidate cache for all assets which depends on that hierarchy
            InvalidateCacheFor(sourceFile);
        }

        public void OnHierarchyRemoved(IPsiSourceFile sourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement)
        {
            InvalidateCacheFor(sourceFile);
        }

        public void InvalidateCacheFor(IPsiSourceFile sourceFile)
        {
            myShellLocks.AssertWriteAccessAllowed();
            var guid = myMetaFileGuidCache.GetAssetGuid(sourceFile);
            if (guid == null) // we have already clear content due to advice on GuidChanged in consructor
                return;

            var visited = new HashSet<Guid>();
            foreach (var deps in myDependencies.GetValuesSafe(guid.Value))
            {
                InvalidateImportCache(deps, visited);
            }

            InvalidateImportCache(guid.Value, visited);
        }

        private void InvalidateImportCache(Guid deps, HashSet<Guid> visited)
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

        public IDictionary<long, IHierarchyElement> GetImportedElementsFor(Guid ownerGuid,
            AssetDocumentHierarchyElement assetDocumentHierarchyElement)
        {
            myShellLocks.AssertReadAccessAllowed();
            if (!myCache.TryGetFromCache(ownerGuid, out var result))
            {
                lock (myLockObject)
                {
                    if (myCache.TryGetFromCache(ownerGuid, out result))
                        return result;

                    result = DoImport(ownerGuid, assetDocumentHierarchyElement, new HashSet<Guid>());
                    StoreResult(ownerGuid, result);
                }
            }

            return result;
        }

        [NotNull]
        private IDictionary<long, IHierarchyElement> DoImport(Guid ownerGuid, AssetDocumentHierarchyElement assetDocumentHierarchyElement, HashSet<Guid> visitedGuid)
        {
            var result = new Dictionary<long, IHierarchyElement>();
            foreach (var prefabInstanceHierarchy in assetDocumentHierarchyElement.GetPrefabInstanceHierarchies())
            {
                Interruption.Current.CheckAndThrow();

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
                        importedElements = DoImport(guid, prefabHierarchy, visitedGuid);
                        StoreResult(guid, importedElements);
                    }
                    else
                    {
                        importedElements = EmptyDictionary<long, IHierarchyElement>.Instance;
                    }
                }

                foreach (var element in prefabHierarchy.Elements())
                {
                    if (element is IStrippedHierarchyElement)
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
                    Assertion.Assert(!(element is IStrippedHierarchyElement), "element should be imported");
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
                var reference = transform?.OwningGameObject;
                if (reference == null)
                    continue;

                var importedGameObject = result.GetValueSafe(reference.Value.LocalDocumentAnchor) as ImportedGameObjectHierarchy;
                if (importedGameObject == null)
                    continue;

                importedGameObject.TransformHierarchy = transform;

            }

            return result;
        }


        private void StoreResult(Guid ownerGuid, [NotNull]IDictionary<long, IHierarchyElement> hierarchyElements)
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