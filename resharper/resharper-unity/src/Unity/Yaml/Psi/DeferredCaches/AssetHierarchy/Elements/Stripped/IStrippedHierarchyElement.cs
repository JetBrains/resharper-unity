using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Stripped
{
    public interface IStrippedHierarchyElement : IHierarchyElement
    {
        LocalReference PrefabInstance { get; }
        ExternalReference CorrespondingSourceObject { get; }
    }
}