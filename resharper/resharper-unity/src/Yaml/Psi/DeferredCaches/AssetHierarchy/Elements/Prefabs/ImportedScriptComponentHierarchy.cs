using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs
{
    public class ImportedScriptComponentHierarchy : IScriptComponentHierarchy
    {
        private readonly IPrefabInstanceHierarchy myPrefabInstanceHierarchy;
        private readonly IScriptComponentHierarchy myScriptComponentHierarchy;

        public ImportedScriptComponentHierarchy(IPrefabInstanceHierarchy prefabInstanceHierarchy,
            IScriptComponentHierarchy scriptComponentHierarchy)
        {
            myPrefabInstanceHierarchy = prefabInstanceHierarchy;
            myScriptComponentHierarchy = scriptComponentHierarchy;
        }

        public LocalReference Location => myScriptComponentHierarchy.Location.GetImportedReference(myPrefabInstanceHierarchy);

        public LocalReference GameObjectReference => myScriptComponentHierarchy.GameObjectReference.GetImportedReference(myPrefabInstanceHierarchy);
        public bool IsStripped => false;
        public LocalReference PrefabInstance => null;
        public ExternalReference CorrespondingSourceObject => null;
        
        public IHierarchyElement Import(IPrefabInstanceHierarchy prefabInstanceHierarchy)
        {
            return new ImportedScriptComponentHierarchy(prefabInstanceHierarchy, this);
        }

        public ExternalReference ScriptReference => myScriptComponentHierarchy.ScriptReference;
    }
}