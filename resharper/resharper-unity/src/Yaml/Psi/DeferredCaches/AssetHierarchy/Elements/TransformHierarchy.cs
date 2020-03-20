using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    public struct TransformHierarchy : ITransformHierarchy
    {
        private readonly ReferenceIndex myLocation;
        private readonly ReferenceIndex myOwner;
        private readonly ReferenceIndex myParent;
        private readonly int myRootIndex;

        public TransformHierarchy(ReferenceIndex location, ReferenceIndex owner, ReferenceIndex parent, int rootIndex)
        {
            myLocation = location;
            myOwner = owner;
            myParent = parent;
            myRootIndex = rootIndex;
        }

        public LocalReference GetLocation(UnityInterningCache cache) => cache.GetReference<LocalReference>(myLocation);

        public IHierarchyElement Import(UnityInterningCache cache, IPrefabInstanceHierarchy prefabInstanceHierarchy)
        {
            return new ImportedTransformHierarchy(prefabInstanceHierarchy, this);
        }

        public string GetName(UnityInterningCache cache) => "Transform";

        public LocalReference GetOwner(UnityInterningCache cache) => cache.GetReference<LocalReference>(myOwner);

        public LocalReference GetParent(UnityInterningCache cache) => cache.GetReference<LocalReference>(myParent);
        public int GetRootIndex(UnityInterningCache cache) => myRootIndex;
    }
}