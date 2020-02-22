using System;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;

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

        public LocalReference Location => myGameObjectHierarchy.Location.GetImportedReference(myPrefabInstanceHierarchy);
        public LocalReference GameObjectReference => null;
        public bool IsStripped => false;
        public LocalReference PrefabInstance => null;
        public ExternalReference CorrespondingSourceObject => null;
        public IHierarchyElement Import(IPrefabInstanceHierarchy prefabInstanceHierarchy) => new ImportedGameObjectHierarchy(prefabInstanceHierarchy, this);

        public string Name
        {
            get
            {
                if (myPrefabInstanceHierarchy.Modifications.TryGetValue((myGameObjectHierarchy.Location.LocalDocumentAnchor, "m_Name"), out var result) && result is AssetSimpleValue simpleValue)
                {
                    return simpleValue.SimpleValue;
                }

                return myGameObjectHierarchy.Name;
            }
        }

        public ITransformHierarchy GetTransformHierarchy(AssetDocumentHierarchyElement owner)
        {
            return TransformHierarchy;
        }
        
        internal ITransformHierarchy TransformHierarchy { get;  set; }
    }
}