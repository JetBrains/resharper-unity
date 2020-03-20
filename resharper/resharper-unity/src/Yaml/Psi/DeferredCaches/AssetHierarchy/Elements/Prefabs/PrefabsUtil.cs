using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs
{
    public static class PrefabsUtil
    {
        public static LocalReference GetImportedReference(this LocalReference localReference, UnityInterningCache cache,
            IPrefabInstanceHierarchy prefabInstanceHierarchy) =>
            localReference == null
                ? null
                : new LocalReference(prefabInstanceHierarchy.GetLocation(cache).OwnerId,
                    Import(prefabInstanceHierarchy.GetLocation(cache).LocalDocumentAnchor, localReference.LocalDocumentAnchor));

        public static ulong Import(ulong prefabInstance, ulong id) => (prefabInstance ^ id) & 0x7fffffffffffffff;
    }
}