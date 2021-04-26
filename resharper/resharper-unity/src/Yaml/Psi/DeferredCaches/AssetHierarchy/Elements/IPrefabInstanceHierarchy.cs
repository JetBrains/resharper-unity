using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    public interface IPrefabInstanceHierarchy : IHierarchyElement
    {
        PrefabModification GetModificationFor(long owningObject, string fieldName);
        IReadOnlyList<PrefabModification> PrefabModifications { get; }
        LocalReference ParentTransform { get; }
        Guid SourcePrefabGuid { get; }
    }
}