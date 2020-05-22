using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;

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

        public LocalReference Location => myComponentHierarchy.Location.GetImportedReference(myPrefabInstanceHierarchy);

        public IHierarchyElement Import(IPrefabInstanceHierarchy prefabInstanceHierarchy)
        {
            return new ImportedComponentHierarchy(prefabInstanceHierarchy, this);
        }

        public string Name => myComponentHierarchy.Name;

        public LocalReference OwningGameObject => myComponentHierarchy.OwningGameObject.GetImportedReference(myPrefabInstanceHierarchy);
    }
}