using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy
{
    public class AssetHierarchyUtil
    {
        private static void GetSelfAndOriginalGameObjectsInternal(LocalReference reference, AssetDocumentHierarchyElementContainer hierarchyElementContainer, ICollection<LocalReference> results)
        {
            var he = hierarchyElementContainer.GetHierarchyElement(reference, true);
            if (he is ImportedGameObjectHierarchy importedGameObjectHierarchy)
            {
                GetSelfAndOriginalGameObjectsInternal(importedGameObjectHierarchy.OriginalGameObject.Location, hierarchyElementContainer, results);
            }
            else if (he is ScriptComponentHierarchy scriptComponentHierarchy)
            {
                GetSelfAndOriginalGameObjectsInternal(scriptComponentHierarchy.OwningGameObject, hierarchyElementContainer, results);
            }

            results.Add(reference);
        }

        public static List<LocalReference> GetSelfAndOriginalGameObjects(LocalReference reference,
            AssetDocumentHierarchyElementContainer hierarchyElementContainer)
        {
            var results = new List<LocalReference>();
            GetSelfAndOriginalGameObjectsInternal(reference, hierarchyElementContainer, results);
            return results;
        }
    }
}