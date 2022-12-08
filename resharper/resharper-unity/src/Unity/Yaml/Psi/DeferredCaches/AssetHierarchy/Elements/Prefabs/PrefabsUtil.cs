using System.Linq;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Util.Extension;

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
        
        
        public static bool TryGetDataIndex(string[] parts, out int index)
        {
            index = 0;
            var dataPart = parts.LastOrDefault(t => t.StartsWith("data"));
            if (dataPart == null)
                return false;

            if (!int.TryParse(dataPart.RemoveStart("data[").RemoveEnd("]"), out var i))
                return false;

            index = i;
            return true;
        }

        public static (string unityEventName, string[] parts) SplitPropertyPath(string modificationPropertyPath)
        {
            var splitPropertyPath = modificationPropertyPath.Split(".m_PersistentCalls.");
            var unityEventName = splitPropertyPath.First();
            var parts = splitPropertyPath.Last().Split('.');
            return (unityEventName, parts);
        }
    }
}