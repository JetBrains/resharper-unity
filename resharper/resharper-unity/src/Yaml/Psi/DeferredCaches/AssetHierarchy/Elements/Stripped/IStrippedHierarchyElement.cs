using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Stripped
{
    public interface IStrippedHierarchyElement : IHierarchyElement
    {
        LocalReference GetPrefabInstance(UnityInterningCache cache);
        ExternalReference GetCoresspondingSourceObject(UnityInterningCache cache);
    }
}