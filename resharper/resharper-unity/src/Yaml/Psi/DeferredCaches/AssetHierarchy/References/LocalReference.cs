using System;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References
{
    public readonly struct LocalReference : IHierarchyReference
    {
        public LocalReference(long ownerId, ulong localDocumentAnchor)
        {
            OwnerId = ownerId;
            LocalDocumentAnchor = localDocumentAnchor;
        }

        public ulong LocalDocumentAnchor { get; }
        
        public long OwnerId { get;}
        public static LocalReference Null { get; set; } = new LocalReference(0, 0);

        public bool Equals(LocalReference other)
        {
            return LocalDocumentAnchor == other.LocalDocumentAnchor && OwnerId == other.OwnerId;
        }

        public override bool Equals(object obj)
        {
            return obj is LocalReference other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (LocalDocumentAnchor.GetHashCode() * 397) ^ OwnerId.GetHashCode();
            }
        }
    }
}