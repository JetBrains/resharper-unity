using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    public struct ScriptComponentHierarchy : IScriptComponentHierarchy
    {
        private readonly ReferenceIndex myLocation;
        private readonly ReferenceIndex myOwnerIndex;
        private readonly ReferenceIndex myScriptReference;

        public ScriptComponentHierarchy(ReferenceIndex location, ReferenceIndex ownerIndex, ReferenceIndex scriptReference)
        {
            myLocation = location;
            myOwnerIndex = ownerIndex;
            myScriptReference = scriptReference;
        }

        public LocalReference GetLocation(UnityInterningCache cache) => cache.GetReference<LocalReference>(myLocation);
        public IHierarchyElement Import(UnityInterningCache cache, IPrefabInstanceHierarchy prefabInstanceHierarchy)
        {
            return new ImportedScriptComponentHierarchy(prefabInstanceHierarchy, this);
        }

        public string GetName(UnityInterningCache cache) => "MonoBehaviour";

        public LocalReference GetOwner(UnityInterningCache cache) => cache.GetReference<LocalReference>(myOwnerIndex);

        public ExternalReference GetScriptReference(UnityInterningCache cache) => cache.GetReference<ExternalReference>(myScriptReference);
    }
}