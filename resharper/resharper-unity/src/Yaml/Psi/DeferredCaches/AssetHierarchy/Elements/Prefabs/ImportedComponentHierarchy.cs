using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs
{
    public class ImportedComponentHierarchy : IComponentHierarchy
    {
        private readonly IPrefabInstanceHierarchy myPrefabInstanceHierarchy;
        private readonly IComponentHierarchy myComponentHierarchy;

        public ImportedComponentHierarchy(IPrefabInstanceHierarchy prefabInstanceHierarchy, IComponentHierarchy componentHierarchy)
        {
            myPrefabInstanceHierarchy = prefabInstanceHierarchy;
            myComponentHierarchy = componentHierarchy;
        } 
        public LocalReference GetLocation(UnityInterningCache cache)
        {
            return myComponentHierarchy.GetLocation(cache).GetImportedReference(cache, myPrefabInstanceHierarchy);
        }

        public IHierarchyElement Import(UnityInterningCache cache, IPrefabInstanceHierarchy prefabInstanceHierarchy)
        {
            return new ImportedComponentHierarchy(prefabInstanceHierarchy, this);
        }

        public string GetName(UnityInterningCache cache) => myComponentHierarchy.GetName(cache);

        public LocalReference GetOwner(UnityInterningCache cache)
        {
            return myComponentHierarchy.GetOwner(cache).GetImportedReference(cache, myPrefabInstanceHierarchy);
        }
    }
}