using System;
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

        public LocalReference GameObjectReference => myComponentHierarchy.GameObjectReference?.GetImportedReference(myPrefabInstanceHierarchy);
        public bool IsStripped => false;
        public LocalReference PrefabInstance => null;
        public ExternalReference CorrespondingSourceObject => null;
        public IHierarchyElement Import(IPrefabInstanceHierarchy prefabInstanceHierarchy)
        {
            return new ImportedComponentHierarchy(prefabInstanceHierarchy, this);
        }

        public string Name => myComponentHierarchy.Name;
    }
}