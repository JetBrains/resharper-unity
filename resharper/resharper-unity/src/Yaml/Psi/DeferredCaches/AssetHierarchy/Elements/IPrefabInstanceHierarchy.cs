using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    public interface IPrefabInstanceHierarchy : IHierarchyElement
    {
        IReadOnlyDictionary<(ulong, string), IAssetValue> Modifications { get; }
        IReadOnlyList<PrefabModification> PrefabModifications { get; }
        LocalReference GetParentTransform(UnityInterningCache cache);
        string SourcePrefabGuid { get; }

    }
}