using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;

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

        public IHierarchyElement Import(IPrefabInstanceHierarchy prefabInstanceHierarchy) => new ImportedTransformHierarchy(prefabInstanceHierarchy, this);

        public string Name => myTransformHierarchy.Name;

        public LocalReference OwningGameObject => myTransformHierarchy.OwningGameObject.GetImportedReference(myPrefabInstanceHierarchy);

        public LocalReference ParentTransform
        {
            get
            {
                if (myTransformHierarchy.ParentTransform.LocalDocumentAnchor == 0)
                    return myPrefabInstanceHierarchy.ParentTransform;

                return myTransformHierarchy.ParentTransform.GetImportedReference(myPrefabInstanceHierarchy);
            }
        }

        public int RootIndex
        {
            get
            {
                var modification = myPrefabInstanceHierarchy.GetModificationFor(myTransformHierarchy.Location.LocalDocumentAnchor, "m_RootOrder");
                if (modification?.Value is AssetSimpleValue simpleValue)
                {
                    if (int.TryParse(simpleValue.SimpleValue, out var index))
                        return index;
                }

                return myTransformHierarchy.RootIndex;
            }
        }
    }
}