using System;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Serialization;
using JetBrains.Util.Maths;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References
{
    public readonly struct LocalReference : IHierarchyReference
    {
        public LocalReference(OWORD owningPsiPersistentIndex, long localDocumentAnchor)
        {
            OwningPsiPersistentIndex = owningPsiPersistentIndex;
            LocalDocumentAnchor = localDocumentAnchor;
        }

        public long LocalDocumentAnchor { get; }
        
        public OWORD OwningPsiPersistentIndex { get;}
        public static LocalReference Null { get; set; } = new LocalReference(0, 0);

        public bool Equals(LocalReference other)
        {
            return LocalDocumentAnchor == other.LocalDocumentAnchor && OwningPsiPersistentIndex == other.OwningPsiPersistentIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is LocalReference other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (LocalDocumentAnchor.GetHashCode() * 397) ^ OwningPsiPersistentIndex.GetHashCode();
            }
        }
    }
}