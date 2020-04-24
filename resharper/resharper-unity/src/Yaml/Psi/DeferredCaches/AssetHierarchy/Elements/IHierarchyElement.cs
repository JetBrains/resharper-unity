using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    public interface IHierarchyElement
    {
        [NotNull]
        LocalReference Location { get; }
        
        [CanBeNull]
        IHierarchyElement Import(IPrefabInstanceHierarchy prefabInstanceHierarchy);
    }
}