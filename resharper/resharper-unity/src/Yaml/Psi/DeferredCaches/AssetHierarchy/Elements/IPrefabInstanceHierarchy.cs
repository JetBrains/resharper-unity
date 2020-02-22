using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    public interface IPrefabInstanceHierarchy : IHierarchyElement
    {
        IReadOnlyList<PrefabModification> PrefabModifications { get; }
        LocalReference ParentTransform { get; }
        string SourcePrefabGuid { get; }

    }
}