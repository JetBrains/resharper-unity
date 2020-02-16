using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Psi.Caches.Persistence;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy
{
    // TODO: use visitor pattern in hierarchy elements?
    [SolutionComponent]
    public class AssetHierarchyProcessor
    {
        private readonly MetaFileGuidCache myMetaFileGuidCache;
        private readonly IShellLocks myLocks;
        private readonly PersistentIndexManager myPersistentIndexManager;
        private readonly AssetDocumentHierarchyElementContainer myAssetDocumentHierarchyElementContainer;

        public AssetHierarchyProcessor(MetaFileGuidCache metaFileGuidCache, IShellLocks locks,
            PersistentIndexManager persistentIndexManager, AssetDocumentHierarchyElementContainer assetDocumentHierarchyElementContainer)
        {
            myMetaFileGuidCache = metaFileGuidCache;
            myLocks = locks;
            myPersistentIndexManager = persistentIndexManager;
            myAssetDocumentHierarchyElementContainer = assetDocumentHierarchyElementContainer;
        }
        
        public void ProcessSceneHierarchyFromComponentToRoot(IHierarchyElement hierarchyElement, IGameObjectConsumer consumer)
        {
            if (hierarchyElement == null)
                return;
            
            myLocks.AssertReadAccessAllowed();
            Assertion.Assert(!hierarchyElement.IsStripped, "TODO: prefab support");
            Assertion.Assert(!(hierarchyElement is PrefabInstanceHierarchy), "Process should not be started from prefab instance, use corresponding GO");
            if (hierarchyElement is GameObjectHierarchy gameObjectHierarchy)
            {
                consumer.AddGameObject(gameObjectHierarchy);
            }
            else if (hierarchyElement is ComponentHierarchy componentHierarchy)
            {
                var gameObjectReference = componentHierarchy.GameObjectReference;
                var gameObject = myAssetDocumentHierarchyElementContainer.GetHierarchyElement(gameObjectReference) as GameObjectHierarchy;
                if (gameObject == null)
                    return;
                consumer.AddGameObject(gameObject);
                var transform = gameObject.Transform;
                if (transform == null)
                    return;
                ProcessSceneHierarchyFromComponentToRoot(transform, consumer);
            } else
            {
                Assertion.Fail($"Unsupported type: {hierarchyElement.GetType().Name}");
            }
        }
    }
}