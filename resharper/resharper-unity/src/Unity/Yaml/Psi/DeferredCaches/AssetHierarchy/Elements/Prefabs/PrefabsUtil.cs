using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs
{
    public static class PrefabsUtil
    {
        public static LocalReference GetImportedReference(this LocalReference localReference, IPrefabInstanceHierarchy prefabInstanceHierarchy)
        {
            if (localReference.LocalDocumentAnchor == 0)
                return localReference;
            return  new LocalReference(prefabInstanceHierarchy.Location.OwningPsiPersistentIndex,
                    GetImportedDocumentAnchor(prefabInstanceHierarchy.Location.LocalDocumentAnchor, localReference.LocalDocumentAnchor));
        }

        // formula for calculating id for component/gameobject after importing to prefab/scene
        public static long GetImportedDocumentAnchor(long prefabInstance, long id) => (prefabInstance ^ id) & 0x7fffffffffffffff;
    }
}