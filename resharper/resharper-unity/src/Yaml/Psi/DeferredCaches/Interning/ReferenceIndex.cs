using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning
{
    public struct ReferenceIndex
    {
        private readonly int myHash;
        
        public ReferenceIndex(IHierarchyReference reference)
        {
            myHash = reference?.GetHashCode() ?? 0;
        }

        private ReferenceIndex(int hash)
        {
            myHash = hash;
        }
        
        [Pure]
        public bool Equals(ReferenceIndex other)
        {
            return myHash == other.myHash;
        }

        public override bool Equals(object obj)
        {
            return obj is ReferenceIndex other && Equals(other);
        }

        public override int GetHashCode()
        {
            return myHash;
        }


        public static ReferenceIndex Read(UnsafeReader reader)
        {
            
            return new ReferenceIndex(reader.ReadInt32());
        }

        public static void Write(UnsafeWriter writer, ReferenceIndex value)
        {
            writer.Write(value.myHash);
        }
    }
}