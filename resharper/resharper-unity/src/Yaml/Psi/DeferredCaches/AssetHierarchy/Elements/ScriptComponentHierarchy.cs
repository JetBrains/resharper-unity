using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    public readonly struct ScriptComponentHierarchy : IScriptComponentHierarchy
    {
        public LocalReference Location { get; }
        public LocalReference Owner { get; }
        public ExternalReference ScriptReference { get; }

        public ScriptComponentHierarchy(LocalReference location, LocalReference owner, ExternalReference scriptReference)
        {
            Location = location;
            Owner = owner;
            ScriptReference = scriptReference;
        }

        public IHierarchyElement Import(IPrefabInstanceHierarchy prefabInstanceHierarchy)
        {
            return new ImportedScriptComponentHierarchy(prefabInstanceHierarchy, this);
        }

        public string Name => "MonoBehaviour";

        public static void Write(UnsafeWriter writer, ScriptComponentHierarchy scriptComponentHierarchy)
        {
            scriptComponentHierarchy.Location.WriteTo(writer);
            scriptComponentHierarchy.Owner.WriteTo(writer);
            scriptComponentHierarchy.ScriptReference.WriteTo(writer);
        }

        public static ScriptComponentHierarchy Read(UnsafeReader reader)
        {
            return new ScriptComponentHierarchy(
                HierarchyReferenceUtil.ReadLocalReferenceFrom(reader),
                HierarchyReferenceUtil.ReadLocalReferenceFrom(reader),
                HierarchyReferenceUtil.ReadExternalReferenceFrom(reader));
        }
    }
}