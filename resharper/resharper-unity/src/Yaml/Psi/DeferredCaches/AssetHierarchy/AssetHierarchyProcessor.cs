using System.Collections.Generic;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy
{
    [SolutionComponent]
    public class AssetHierarchyProcessor
    {
        private readonly DeferredCachesLocks myLocks;
        private readonly PrefabImportCache myPrefabImportCache;
        private readonly AssetDocumentHierarchyElementContainer myAssetDocumentHierarchyElementContainer;

        public AssetHierarchyProcessor(DeferredCachesLocks locks, PrefabImportCache prefabImportCache, AssetDocumentHierarchyElementContainer assetDocumentHierarchyElementContainer)
        {
            myLocks = locks;
            myPrefabImportCache = prefabImportCache;
            myAssetDocumentHierarchyElementContainer = assetDocumentHierarchyElementContainer;
        }

        public void ProcessSceneHierarchyFromComponentToRoot(IHierarchyElement hierarchyElement, IGameObjectConsumer consumer, bool forcePrefabImport)
        {
            myLocks.AssertReadAccessAllowed();

            if (hierarchyElement == null)
                return;
            
            Assertion.Assert(!hierarchyElement.IsStripped, "!hierarchyElement.IsStripped"); // stripped elements should be never returned, 
            Assertion.Assert(!(hierarchyElement is IPrefabInstanceHierarchy), "Process should not be started from prefab instance, use corresponding GO");

            var owner = myAssetDocumentHierarchyElementContainer.GetAssetHierarchyFor(hierarchyElement.Location, out _);
            
            ProcessHierarchy(owner, hierarchyElement, consumer, forcePrefabImport, new HashSet<ulong>());
        }

        public void ProcessSceneHierarchyFromComponentToRoot(LocalReference location, IGameObjectConsumer consumer, bool forcePrefabImportForStartPoint, bool forcePrefabImport)
        {
            myLocks.AssertReadAccessAllowed();

            var owner = myAssetDocumentHierarchyElementContainer.GetAssetHierarchyFor(location, out var guid);
            if (owner == null)
                return;

            var hierarchyElement = owner.GetHierarchyElement(guid, location.LocalDocumentAnchor, forcePrefabImportForStartPoint ? myPrefabImportCache : null);
            ProcessSceneHierarchyFromComponentToRoot(hierarchyElement, consumer, forcePrefabImport);            
        }

        private void ProcessHierarchy(AssetDocumentHierarchyElement owner, IHierarchyElement element,
            IGameObjectConsumer consumer, bool prefabImport, HashSet<ulong> visited)
        {
            if (element == null)
                return;
            
            if (visited.Contains(element.Location.LocalDocumentAnchor))
                return;
            
            if (element is IGameObjectHierarchy gameObjectHierarchy)
            {
                ProcessGameObject(owner, gameObjectHierarchy, consumer, prefabImport, visited);
            }
            else if (element is IComponentHierarchy componentHierarchy)
            {
                var gameObjectReference = componentHierarchy.GameObjectReference;
                var gameObject = myAssetDocumentHierarchyElementContainer.GetHierarchyElement(gameObjectReference, prefabImport) as IGameObjectHierarchy;

                ProcessGameObject(owner, gameObject, consumer, prefabImport, visited);
            } else
            {
                Assertion.Fail($"Unsupported type: {element.GetType().Name}");
            }
        }

        private void ProcessGameObject(AssetDocumentHierarchyElement owner, IGameObjectHierarchy gameObject,
            IGameObjectConsumer consumer, bool prefabImport, HashSet<ulong> visited)
        {
            var transform = gameObject?.GetTransformHierarchy(owner);
            if (transform == null)
                return;
            
            if (!consumer.AddGameObject(owner, gameObject))
                return;
                
            var parentTransform = myAssetDocumentHierarchyElementContainer.GetHierarchyElement(gameObject.GetTransformHierarchy(owner).Parent, true) as ITransformHierarchy;
            if (parentTransform == null)
                return;

            visited.Add(gameObject.Location.LocalDocumentAnchor);
            ProcessHierarchy(owner, parentTransform, consumer, prefabImport, visited);
        }
    }
}