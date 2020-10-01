using System;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References
{
    public readonly struct ExternalReference : IHierarchyReference
    {
        public ExternalReference(Guid externalAssetGuid, long localDocumentAnchor)
        {
            ExternalAssetGuid = externalAssetGuid;
            LocalDocumentAnchor = localDocumentAnchor;
        }

        // TODO : think about storing pointer to Guid here (this will safe 12 bytes), all guids will be interned in some cache
        public Guid ExternalAssetGuid { get; }
        public long LocalDocumentAnchor { get; }


        public bool Equals(ExternalReference other)
        {
            return ExternalAssetGuid == other.ExternalAssetGuid && LocalDocumentAnchor == other.LocalDocumentAnchor;
        }

        public override bool Equals(object obj)
        {
            return obj is ExternalReference other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ExternalAssetGuid.GetHashCode() * 397) ^ LocalDocumentAnchor.GetHashCode();
            }
        }
    }
}