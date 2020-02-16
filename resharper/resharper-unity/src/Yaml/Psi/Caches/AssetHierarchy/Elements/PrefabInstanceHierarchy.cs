using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.Elements
{
    [PolymorphicMarshaller]
    public class PrefabInstanceHierarchy : IHierarchyElement
    {
        [UsedImplicitly] 
        public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader)
        {
            var location = reader.ReadPolymorphic<LocalReference>();
            var parentTransform = reader.ReadPolymorphic<IHierarchyReference>();
            var count = reader.ReadInt();
            var modifications = new List<PrefabModification>();
            for (int i = 0; i < count; i++)
                modifications.Add(reader.ReadPolymorphic<PrefabModification>());
            return new PrefabInstanceHierarchy(location, parentTransform, modifications);
        }

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as PrefabInstanceHierarchy);

        private static void Write(UnsafeWriter writer, PrefabInstanceHierarchy value)
        {
            writer.WritePolymorphic(value.Location);
            writer.WritePolymorphic(value.ParentTransform);
            writer.Write(value.PrefabModifications.Count);
            foreach (var prefabModification in value.PrefabModifications)
            {
                writer.WritePolymorphic(prefabModification);
            }
            writer.Write(value.IsStripped);
        }

        public PrefabInstanceHierarchy(LocalReference location, IHierarchyReference parentTransform, List<PrefabModification> prefabModifications)
        {
            Location = location;
            ParentTransform = parentTransform;
            PrefabModifications = prefabModifications;
        }

        public IReadOnlyList<PrefabModification> PrefabModifications { get; }
        public IHierarchyReference ParentTransform { get; }
        public LocalReference Location { get; }
        public IHierarchyReference GameObjectReference => null;
        public bool IsStripped => false;
        public LocalReference PrefabInstance => null;
        public ExternalReference CorrespondingSourceObject => null;

        protected bool Equals(PrefabInstanceHierarchy other)
        {
            return Location.Equals(other.Location);
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
            return Location.GetHashCode();
        }
    }
}