using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    public interface IComponentHierarchy : IHierarchyElement
    {
        string Name { get; }
        
        // TODO : think about store only owner anchor, because file id is stored in Location
        LocalReference Owner { get; }
    }
}