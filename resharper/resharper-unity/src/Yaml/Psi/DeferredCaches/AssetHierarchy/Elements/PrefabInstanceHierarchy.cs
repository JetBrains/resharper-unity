using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    [PolymorphicMarshaller]
    public class PrefabInstanceHierarchy : IPrefabInstanceHierarchy
    {
        [UsedImplicitly] 
        public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader)
        {
            var location = reader.ReadPolymorphic<LocalReference>();
            var sourcePrefabGuid = reader.ReadString();
            var parentTransform = reader.ReadPolymorphic<LocalReference>();
            var count = reader.ReadInt32();
            var modifications = new List<PrefabModification>();
            for (int i = 0; i < count; i++)
                modifications.Add(reader.ReadPolymorphic<PrefabModification>());
            return new PrefabInstanceHierarchy(location, sourcePrefabGuid, parentTransform, modifications);
        }

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as PrefabInstanceHierarchy);

        private static void Write(UnsafeWriter writer, PrefabInstanceHierarchy value)
        {
            writer.WritePolymorphic(value.Location);
            writer.Write(value.SourcePrefabGuid);
            writer.WritePolymorphic(value.ParentTransform);
            writer.Write(value.PrefabModifications.Count);
            foreach (var prefabModification in value.PrefabModifications)
            {
                writer.WritePolymorphic(prefabModification);
            }
        }

        public PrefabInstanceHierarchy(LocalReference location, string sourcePrefabGuid, LocalReference parentTransform, List<PrefabModification> prefabModifications)
        {
            Location = location;
            ParentTransform = parentTransform;
            PrefabModifications = prefabModifications;
            SourcePrefabGuid = sourcePrefabGuid;
        }

        public IReadOnlyList<PrefabModification> PrefabModifications { get; }
        public LocalReference ParentTransform { get; }
        public LocalReference Location { get; }
        public LocalReference GameObjectReference => null;
        public bool IsStripped => false;
        public LocalReference PrefabInstance => null;
        public ExternalReference CorrespondingSourceObject => null;
        public IHierarchyElement Import(IPrefabInstanceHierarchy prefabInstanceHierarchy) => null;
        public string SourcePrefabGuid { get; }
        
        protected bool Equals(PrefabInstanceHierarchy other)
        {
            return Location.Equals(other.Location) && SourcePrefabGuid == other.SourcePrefabGuid;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PrefabInstanceHierarchy) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Location.GetHashCode() * 397) ^ SourcePrefabGuid.GetHashCode();
            }
        }
    }
}