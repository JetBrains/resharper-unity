
using System;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs
{
    public class ImportedTransformHierarchy : ITransformHierarchy
    {
        private readonly IPrefabInstanceHierarchy myPrefabInstanceHierarchy;
        private readonly ITransformHierarchy myTransformHierarchy;

        public ImportedTransformHierarchy(IPrefabInstanceHierarchy prefabInstanceHierarchy, ITransformHierarchy transformHierarchy)
        {
            myPrefabInstanceHierarchy = prefabInstanceHierarchy;
            myTransformHierarchy = transformHierarchy;
        }

        public LocalReference GetLocation(UnityInterningCache cache)
        {
            return myTransformHierarchy.GetLocation(cache).GetImportedReference(cache, myPrefabInstanceHierarchy);
        }

        public IHierarchyElement Import(UnityInterningCache cache, IPrefabInstanceHierarchy prefabInstanceHierarchy)
        {
            return new ImportedTransformHierarchy(prefabInstanceHierarchy, this);
        }

        public string GetName(UnityInterningCache cache) => myTransformHierarchy.GetName(cache);

        public LocalReference GetOwner(UnityInterningCache cache)
        {
            return myTransformHierarchy.GetOwner(cache).GetImportedReference(cache, myPrefabInstanceHierarchy);
        }

        public LocalReference GetParent(UnityInterningCache cache)
        {
            if (myTransformHierarchy.GetParent(cache).LocalDocumentAnchor == 0)
                return myPrefabInstanceHierarchy.GetParentTransform(cache);
                
            return myTransformHierarchy.GetParent(cache).GetImportedReference(cache, myPrefabInstanceHierarchy);
        }

        public int GetRootIndex(UnityInterningCache cache)
        {
            if (myPrefabInstanceHierarchy.Modifications.TryGetValue((myTransformHierarchy.GetLocation(cache).LocalDocumentAnchor, "m_RootOrder"),
                out var result) && result is AssetSimpleValue simpleValue)
            {
                if (int.TryParse(simpleValue.SimpleValue, out var index))
                    return index;
            }

            return myTransformHierarchy.GetRootIndex(cache);
        }
    }
}