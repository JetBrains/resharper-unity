using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Psi.Caches.Persistence;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy
{
    [SolutionComponent]
    public class AssetHierarchyProcessor
    {
        private readonly MetaFileGuidCache myMetaFileGuidCache;
        private readonly DeferredCachesLocks myLocks;
        private readonly PrefabImportCache myPrefabImportCache;
        private readonly PersistentIndexManager myPersistentIndexManager;
        private readonly AssetDocumentHierarchyElementContainer myAssetDocumentHierarchyElementContainer;

        public AssetHierarchyProcessor(MetaFileGuidCache metaFileGuidCache, DeferredCachesLocks locks, PrefabImportCache prefabImportCache,
            PersistentIndexManager persistentIndexManager, AssetDocumentHierarchyElementContainer assetDocumentHierarchyElementContainer)
        {
            myMetaFileGuidCache = metaFileGuidCache;
            myLocks = locks;
            myPrefabImportCache = prefabImportCache;
            myPersistentIndexManager = persistentIndexManager;
            myAssetDocumentHierarchyElementContainer = assetDocumentHierarchyElementContainer;
        }

        public void ProcessSceneHierarchyFromComponentToRoot(IHierarchyElement hierarchyElement, IGameObjectConsumer consumer,
            bool forcePrefabImportForStartPoint, bool forcePrefabImport)
        {
            myLocks.AssertReadAccessAllowed();

            if (hierarchyElement == null)
                return;
            
            Assertion.Assert(!hierarchyElement.IsStripped, "!hierarchyElement.IsStripped"); // stripped elements should be never returned, 
            Assertion.Assert(!(hierarchyElement is IPrefabInstanceHierarchy), "Process should not be started from prefab instance, use corresponding GO");

            var owner = myAssetDocumentHierarchyElementContainer.GetAssetHierarchyFor(hierarchyElement.Location, out _);
            
            ProcessHierarchy(owner, hierarchyElement, consumer);
        }

        public void ProcessSceneHierarchyFromComponentToRoot(LocalReference location, IGameObjectConsumer consumer, bool forcePrefabImportForStartPoint, bool forcePrefabImport)
        {
            myLocks.AssertReadAccessAllowed();

            var owner = myAssetDocumentHierarchyElementContainer.GetAssetHierarchyFor(location, out var guid);
            if (owner == null)
                return;

            var hierarchyElement = owner.GetHierarchyElement(guid, location.LocalDocumentAnchor, forcePrefabImportForStartPoint ? myPrefabImportCache : null);
            ProcessSceneHierarchyFromComponentToRoot(location, consumer, forcePrefabImportForStartPoint, forcePrefabImport);            
  
        }

        private void ProcessHierarchy(AssetDocumentHierarchyElement owner, IHierarchyElement element,
            IGameObjectConsumer consumer)
        {
            if (element == null)
                return;
            
            if (element is IGameObjectHierarchy gameObjectHierarchy)
            {
                ProcessGameObject(owner, gameObjectHierarchy, consumer);
            }
            else if (element is IComponentHierarchy componentHierarchy)
            {
                var gameObjectReference = componentHierarchy.GameObjectReference;
                var gameObject = myAssetDocumentHierarchyElementContainer.GetHierarchyElement(gameObjectReference, false) as IGameObjectHierarchy;

                ProcessGameObject(owner, gameObject, consumer);
            } else
            {
                Assertion.Fail($"Unsupported type: {element.GetType().Name}");
            }
        }

        private void ProcessGameObject(AssetDocumentHierarchyElement owner, IGameObjectHierarchy gameObject, IGameObjectConsumer consumer)
        {
            var transform = gameObject?.GetTransformHierarchy(owner);
            if (transform == null)
                return;
            
            if (!consumer.AddGameObject(owner, gameObject))
                return;
                
            var parentTransform = myAssetDocumentHierarchyElementContainer.GetHierarchyElement(gameObject.GetTransformHierarchy(owner).Parent, true) as ITransformHierarchy;
            if (parentTransform == null)
                return;
            
            ProcessHierarchy(owner, parentTransform, consumer);
        }
    }
}