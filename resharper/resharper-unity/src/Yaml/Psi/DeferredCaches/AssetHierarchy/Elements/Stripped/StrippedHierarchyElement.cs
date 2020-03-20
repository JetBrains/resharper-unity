using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Stripped
{
    public struct StrippedHierarchyElement : IStrippedHierarchyElement
    {
        private readonly ReferenceIndex myLocation;
        private readonly ReferenceIndex myPrefabInstance;
        private readonly ReferenceIndex myCorrespondingSourceObject;

        public StrippedHierarchyElement(ReferenceIndex location, ReferenceIndex prefabInstance, ReferenceIndex correspondingSourceObject)
        {
            myLocation = location;
            myPrefabInstance = prefabInstance;
            myCorrespondingSourceObject = correspondingSourceObject;
        }

        public LocalReference GetLocation(UnityInterningCache cache) => cache.GetReference<LocalReference>(myLocation);

        public IHierarchyElement Import(UnityInterningCache cache, IPrefabInstanceHierarchy prefabInstanceHierarchy) => null;

        public LocalReference GetPrefabInstance(UnityInterningCache cache) => cache.GetReference<LocalReference>(myPrefabInstance);

        public ExternalReference GetCoresspondingSourceObject(UnityInterningCache cache) => cache.GetReference<ExternalReference>(myCorrespondingSourceObject);
    }
}