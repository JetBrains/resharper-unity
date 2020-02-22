
using System;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;

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

        public LocalReference Location => myTransformHierarchy.Location.GetImportedReference(myPrefabInstanceHierarchy);
        public LocalReference GameObjectReference => myTransformHierarchy.GameObjectReference.GetImportedReference(myPrefabInstanceHierarchy);
        public bool IsStripped => false;
        public LocalReference PrefabInstance => null;
        public ExternalReference CorrespondingSourceObject => null;
        public IHierarchyElement Import(IPrefabInstanceHierarchy prefabInstanceHierarchy)
        {
            return new ImportedTransformHierarchy(prefabInstanceHierarchy, this);
        }

        public string Name => myTransformHierarchy.Name;

        public LocalReference Parent
        {
            get
            {
                if (myTransformHierarchy.Parent.LocalDocumentAnchor == 0)
                    return myPrefabInstanceHierarchy.ParentTransform;
                
                return myTransformHierarchy.Location.GetImportedReference(myPrefabInstanceHierarchy);
            }
        }

        public int RootIndex => throw new NotImplementedException("TODO: apply patch");
    }
}