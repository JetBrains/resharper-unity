using JetBrains.Serialization;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    public class UnityEventHandlerCacheItem
    {
        public static readonly IUnsafeMarshaller<UnityEventHandlerCacheItem> Marshaller =
            new UniversalMarshaller<UnityEventHandlerCacheItem>(Read, Write);

        public UnityEventHandlerCacheItem(string assetGuid, string referenceShortName)
        {
            AssetGuid = assetGuid;
            ReferenceShortName = referenceShortName;
        }

        public string AssetGuid { get; }
        public string ReferenceShortName { get; }

        private static UnityEventHandlerCacheItem Read(UnsafeReader reader)
        {
            var assetGuid = reader.ReadString();
            var referenceShortName = reader.ReadString();
            return new UnityEventHandlerCacheItem(assetGuid, referenceShortName);
        }

        private static void Write(UnsafeWriter writer, UnityEventHandlerCacheItem value)
        {
            writer.Write(value.AssetGuid);
            writer.Write(value.ReferenceShortName);
        }

        public override string ToString() => $"{AssetGuid}::{ReferenceShortName}";
    }
}