using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning;

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

        public LocalReference GetLocation(UnityInterningCache cache)
        {
            return myScriptComponentHierarchy.GetLocation(cache).GetImportedReference(cache, myPrefabInstanceHierarchy);
        }

        public IHierarchyElement Import(UnityInterningCache cache, IPrefabInstanceHierarchy prefabInstanceHierarchy)
        {
            return new ImportedScriptComponentHierarchy(prefabInstanceHierarchy, this);
        }

        public string GetName(UnityInterningCache cache) => myScriptComponentHierarchy.GetName(cache);

        public LocalReference GetOwner(UnityInterningCache cache)
        {
            return myScriptComponentHierarchy.GetOwner(cache).GetImportedReference(cache, myPrefabInstanceHierarchy);
        }

        public ExternalReference GetScriptReference(UnityInterningCache cache) => myScriptComponentHierarchy.GetScriptReference(cache);
    }
}