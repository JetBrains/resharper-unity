using System;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References
{
    public readonly struct ExternalReference : IHierarchyReference
    {
        public ExternalReference(Guid externalAssetGuid, ulong localDocumentAnchor)
        {
            ExternalAssetGuid = externalAssetGuid;
            LocalDocumentAnchor = localDocumentAnchor;
        }

        public Guid ExternalAssetGuid { get; }
        public ulong LocalDocumentAnchor { get; }


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