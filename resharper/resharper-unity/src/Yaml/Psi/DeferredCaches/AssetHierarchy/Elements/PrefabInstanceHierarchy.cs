using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    public struct PrefabInstanceHierarchy : IPrefabInstanceHierarchy
    {
        private readonly ReferenceIndex myLocation;
        private readonly ReferenceIndex myParentTransform;
        private readonly Dictionary<(ulong, string), IAssetValue> myModifications;
        public PrefabInstanceHierarchy(ReferenceIndex location, ReferenceIndex parentTransform, List<PrefabModification> prefabModifications, string sourcePrefabGuid)
        {
            myLocation = location;
            myParentTransform = parentTransform;
            PrefabModifications = prefabModifications;
            SourcePrefabGuid = sourcePrefabGuid;
            myModifications  = new Dictionary<(ulong, string), IAssetValue>();

            foreach (var modification in prefabModifications)
            {
                myModifications[(modification.Target.LocalDocumentAnchor, modification.PropertyPath)] = modification.Value;
            }
        }

        public LocalReference GetLocation(UnityInterningCache cache) => cache.GetReference<LocalReference>(myLocation);
        public IReadOnlyDictionary<(ulong, string), IAssetValue> Modifications => myModifications;
        public IReadOnlyList<PrefabModification> PrefabModifications { get; }
        public LocalReference GetParentTransform(UnityInterningCache cache) => cache.GetReference<LocalReference>(myParentTransform);
        public string SourcePrefabGuid { get; }
        public IHierarchyElement Import(UnityInterningCache cache, IPrefabInstanceHierarchy prefabInstanceHierarchy) => null;
    }
}