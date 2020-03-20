using System;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs
{
    public class ImportedGameObjectHierarchy : IGameObjectHierarchy
    {
        private readonly IPrefabInstanceHierarchy myPrefabInstanceHierarchy;
        private readonly IGameObjectHierarchy myGameObjectHierarchy;

        public ImportedGameObjectHierarchy(IPrefabInstanceHierarchy prefabInstanceHierarchy, IGameObjectHierarchy gameObjectHierarchy)
        {
            myPrefabInstanceHierarchy = prefabInstanceHierarchy;
            myGameObjectHierarchy = gameObjectHierarchy;
        }
        
        internal ITransformHierarchy TransformHierarchy { get;  set; }
        public LocalReference GetLocation(UnityInterningCache cache)
        {
            return myGameObjectHierarchy.GetLocation(cache).GetImportedReference(cache, myPrefabInstanceHierarchy);
        }

        public IHierarchyElement Import(UnityInterningCache cache, IPrefabInstanceHierarchy prefabInstanceHierarchy)
        {
            return new ImportedGameObjectHierarchy(prefabInstanceHierarchy, this);
        }

        public string GetName(UnityInterningCache cache)
        {
            if (myPrefabInstanceHierarchy.Modifications.TryGetValue((myGameObjectHierarchy.GetLocation(cache).LocalDocumentAnchor, "m_Name"), out var result) && result is AssetSimpleValue simpleValue)
            {
                return simpleValue.SimpleValue;
            }

            return myGameObjectHierarchy.GetName(cache);
        }

        public ITransformHierarchy GetTransformHierarchy(UnityInterningCache cache, AssetDocumentHierarchyElement owner)
        {
            return TransformHierarchy;
        }
    }
}