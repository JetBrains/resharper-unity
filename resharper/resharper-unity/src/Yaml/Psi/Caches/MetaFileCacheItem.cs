using JetBrains.Serialization;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    public class MetaFileCacheItem
    {
        public static readonly IUnsafeMarshaller<MetaFileCacheItem> Marshaller =
            new UniversalMarshaller<MetaFileCacheItem>(Read, Write);

        private readonly string myGuid;

        public MetaFileCacheItem(string guid)
        {
            myGuid = guid;
        }

        public string Guid => myGuid;

        private static MetaFileCacheItem Read(UnsafeReader reader)
        {
            var guid = reader.ReadString();
            return new MetaFileCacheItem(guid);
        }

        private static void Write(UnsafeWriter writer, MetaFileCacheItem value)
        {
            writer.Write(value.myGuid);
        }

        public override string ToString() => $"guid: {myGuid}";
    }
}