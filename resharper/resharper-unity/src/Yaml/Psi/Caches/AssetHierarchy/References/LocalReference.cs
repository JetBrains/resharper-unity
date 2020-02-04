using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Psi;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.References
{
    [PolymorphicMarshaller]
    public class LocalReference : IHierarchyReference
    {
        [UsedImplicitly] 
        public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader) => new LocalReference(reader.ReadString(), reader.ReadString());

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as LocalReference);

        private static void Write(UnsafeWriter writer, LocalReference value)
        {
            writer.Write(value.OwnerId);
            writer.Write(value.LocalDocumentAnchor);
        }
        
        public LocalReference(string ownerId, string localDocumentAnchor)
        {
            OwnerId = ownerId;
            LocalDocumentAnchor = localDocumentAnchor;
        }

        public string LocalDocumentAnchor { get; }
        
        public string OwnerId { get; private set; }

    }
}