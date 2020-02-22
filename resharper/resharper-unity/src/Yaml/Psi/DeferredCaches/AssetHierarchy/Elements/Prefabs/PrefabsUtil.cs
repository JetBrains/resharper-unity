using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs
{
    public static class PrefabsUtil
    {
        public static LocalReference GetImportedReference(this LocalReference localReference,
            IPrefabInstanceHierarchy prefabInstanceHierarchy) =>
            localReference == null
                ? null
                : new LocalReference(prefabInstanceHierarchy.Location.OwnerId,
                    Import(prefabInstanceHierarchy.Location.LocalDocumentAnchor, localReference.LocalDocumentAnchor));

        public static ulong Import(ulong prefabInstance, ulong id) => (prefabInstance ^ id) & 0x7fffffffffffffff;
    }
}