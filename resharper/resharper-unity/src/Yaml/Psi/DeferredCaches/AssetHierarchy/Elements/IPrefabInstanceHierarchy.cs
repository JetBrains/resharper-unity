using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    public interface IPrefabInstanceHierarchy : IHierarchyElement
    {
        IReadOnlyDictionary<string, IReadOnlyDictionary<ulong, PrefabModification>> Modifications { get; }
        IReadOnlyList<PrefabModification> PrefabModifications { get; }
        LocalReference ParentTransform { get; }
        Guid SourcePrefabGuid { get; }
    }
}