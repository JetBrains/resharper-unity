using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.References
{
    [PolymorphicMarshaller]
    public class ExternalReference : IHierarchyReference
    {
        [UsedImplicitly] 
        public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader)
        {
            return new ExternalReference(reader.ReadString(), reader.ReadString());
        }

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as ExternalReference);

        private static void Write(UnsafeWriter writer, ExternalReference value)
        {
            writer.Write(value.ExternalAssetGuid);
            writer.Write(value.LocalDocumentAnchor);
        }

        public ExternalReference(string externalAssetGuid, string localDocumentAnchor)
        {
            ExternalAssetGuid = externalAssetGuid;
            LocalDocumentAnchor = localDocumentAnchor;
        }

        private string ExternalAssetGuid { get; }
        public string LocalDocumentAnchor { get; }
    }
}