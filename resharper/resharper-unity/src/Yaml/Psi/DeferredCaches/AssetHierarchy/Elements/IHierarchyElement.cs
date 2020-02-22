using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    public interface IHierarchyElement
    {
        [NotNull]
        LocalReference Location { get; }
        [CanBeNull]
        LocalReference GameObjectReference { get; }
        bool IsStripped { get; }
        [CanBeNull]
        LocalReference PrefabInstance { get; }
        [CanBeNull]
        ExternalReference CorrespondingSourceObject { get; }
        IHierarchyElement Import(IPrefabInstanceHierarchy prefabInstanceHierarchy);
    }
}