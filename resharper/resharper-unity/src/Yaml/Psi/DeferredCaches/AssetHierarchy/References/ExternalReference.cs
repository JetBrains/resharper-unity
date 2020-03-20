using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Serialization;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References
{
    [PolymorphicMarshaller]
    public class ExternalReference : IHierarchyReference
    {
        [UsedImplicitly] 
        public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader)
        {
            return new ExternalReference(reader.ReadString(), reader.ReadULong());
        }

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as ExternalReference);

        private static void Write(UnsafeWriter writer, ExternalReference value)
        {
            writer.Write(value.ExternalAssetGuid);
            writer.Write(value.LocalDocumentAnchor);
        }

        public ExternalReference(string externalAssetGuid, ulong localDocumentAnchor)
        {
            ExternalAssetGuid = externalAssetGuid;
            LocalDocumentAnchor = localDocumentAnchor;
        }

        public string ExternalAssetGuid { get; }
        public ulong LocalDocumentAnchor { get; }


        protected bool Equals(ExternalReference other)
        {
            return ExternalAssetGuid == other.ExternalAssetGuid && LocalDocumentAnchor == other.LocalDocumentAnchor;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ExternalReference) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ExternalAssetGuid.GetPlatformIndependentHashCode() * 397) ^ LocalDocumentAnchor.GetHashCode();
            }
        }
    }
}