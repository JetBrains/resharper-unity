using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    public struct ComponentHierarchy : IComponentHierarchy
    {
        private readonly ReferenceIndex myLocation;
        private readonly ReferenceIndex myOwner;
        private readonly StringIndex myName;

        public ComponentHierarchy(ReferenceIndex location, ReferenceIndex owner, StringIndex name)
        {
            myLocation = location;
            myOwner = owner;
            myName = name;
        }

        public LocalReference GetLocation(UnityInterningCache cache) => cache.GetReference<LocalReference>(myLocation);

        public IHierarchyElement Import(UnityInterningCache cache, IPrefabInstanceHierarchy prefabInstanceHierarchy)
        {
            return new ImportedComponentHierarchy(prefabInstanceHierarchy, this);
        }

        public string GetName(UnityInterningCache cache) => cache.GetString(myName);

        public LocalReference GetOwner(UnityInterningCache cache) => cache.GetReference<LocalReference>(myOwner);
    }
}