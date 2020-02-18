using System;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.References
{
    [PolymorphicMarshaller]
    public class LocalReference : IHierarchyReference
    {
        [UsedImplicitly] 
        public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader) => new LocalReference(reader.ReadInt32(), reader.ReadString());

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as LocalReference);

        private static void Write(UnsafeWriter writer, LocalReference value)
        {
            writer.Write(value.OwnerId);
            writer.Write(value.LocalDocumentAnchor);
        }
        
        public LocalReference(int ownerId, string localDocumentAnchor)
        {
            OwnerId = ownerId;
            LocalDocumentAnchor = localDocumentAnchor;
        }

        public string LocalDocumentAnchor { get; }
        
        public int OwnerId { get;}
        public static LocalReference Null { get; set; } = new LocalReference(0, String.Empty);

        protected bool Equals(LocalReference other)
        {
            if (LocalDocumentAnchor.Equals("0") && other.LocalDocumentAnchor.Equals("0"))
                return true;
            return LocalDocumentAnchor == other.LocalDocumentAnchor && OwnerId == other.OwnerId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((LocalReference) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                if (LocalDocumentAnchor.Equals("0"))
                    return 0;
                return (LocalDocumentAnchor.GetHashCode() * 397) ^ OwnerId.GetHashCode();
            }
        }
    }
}