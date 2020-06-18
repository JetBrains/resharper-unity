using System;
using System.Collections.Generic;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    public readonly struct PrefabInstanceHierarchy : IPrefabInstanceHierarchy
    {
        private readonly Dictionary<string, IReadOnlyDictionary<ulong, PrefabModification>> myModifications;
        public PrefabInstanceHierarchy(LocalReference location, LocalReference parentTransform, List<PrefabModification> prefabModifications, Guid sourcePrefabGuid)
        {
            Location = location;
            ParentTransform = parentTransform;
            PrefabModifications = prefabModifications;
            SourcePrefabGuid = sourcePrefabGuid;
            myModifications  = new Dictionary<string, IReadOnlyDictionary<ulong, PrefabModification>> ();

            foreach (var modification in prefabModifications)
            {
                Dictionary<ulong, PrefabModification> dictionary;
                if (myModifications.TryGetValue(modification.PropertyPath, out var rDictionary))
                {
                    dictionary = (Dictionary<ulong, PrefabModification>) rDictionary;
                }
                else
                {
                    dictionary = new Dictionary<ulong, PrefabModification>();
                    myModifications[modification.PropertyPath] = dictionary;
                }
                
                dictionary[modification.Target.LocalDocumentAnchor] = modification;
            }
        }

        public LocalReference Location { get; }
        public LocalReference ParentTransform { get; }
        public PrefabModification GetModificationFor(ulong owningObject, string fieldName)
        {
            if (myModifications.TryGetValue(fieldName, out var result) &&
                result.TryGetValue(owningObject, out var modification))
                return modification;
            return null;
        }

        public IReadOnlyList<PrefabModification> PrefabModifications { get; }
        public Guid SourcePrefabGuid { get; }

        public IHierarchyElement Import(IPrefabInstanceHierarchy prefabInstanceHierarchy) => null;

        public static void Write(UnsafeWriter writer, PrefabInstanceHierarchy prefabInstanceHierarchy)
        {
            prefabInstanceHierarchy.Location.WriteTo(writer);
            prefabInstanceHierarchy.ParentTransform.WriteTo(writer);
            
            writer.Write(prefabInstanceHierarchy.PrefabModifications.Count);
            foreach (var prefabModification in prefabInstanceHierarchy.PrefabModifications)
            {
                writer.WritePolymorphic(prefabModification);
            }

            writer.Write(prefabInstanceHierarchy.SourcePrefabGuid);
        }

        public static PrefabInstanceHierarchy Read(UnsafeReader reader)
        {
            var location = HierarchyReferenceUtil.ReadLocalReferenceFrom(reader);
            var parentTransform = HierarchyReferenceUtil.ReadLocalReferenceFrom(reader);
            var count = reader.ReadInt32();
            var modifications = new List<PrefabModification>();
            for (int i = 0; i < count; i++)
                modifications.Add(reader.ReadPolymorphic<PrefabModification>());
            
            var sourcePrefabGuid = reader.ReadGuid();
            return new PrefabInstanceHierarchy(location, parentTransform, modifications, sourcePrefabGuid);
        }
    }
}