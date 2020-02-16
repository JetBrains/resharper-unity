using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.References;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.Elements
{
    public interface IHierarchyElement
    {
        [NotNull]
        LocalReference Location { get; }
        [CanBeNull]
        IHierarchyReference GameObjectReference { get; }
        bool IsStripped { get; }
        [CanBeNull]
        LocalReference PrefabInstance { get; }
        [CanBeNull]
        ExternalReference CorrespondingSourceObject { get; }
    }
}