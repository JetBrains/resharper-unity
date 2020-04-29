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

        public IHierarchyElement Import(IPrefabInstanceHierarchy prefabInstanceHierarchy)
        {
            return new ImportedScriptComponentHierarchy(prefabInstanceHierarchy, this);
        }

        public string Name => myScriptComponentHierarchy.Name;

        public LocalReference Owner => myScriptComponentHierarchy.Owner.GetImportedReference(myPrefabInstanceHierarchy);

        public ExternalReference ScriptReference => myScriptComponentHierarchy.ScriptReference;

        public LocalReference OriginLocation
        {
            get
            {
                if (myScriptComponentHierarchy is ImportedScriptComponentHierarchy importedScriptComponentHierarchy)
                    return importedScriptComponentHierarchy.OriginLocation;
                return myScriptComponentHierarchy.Location;
            }
        }
    }
}