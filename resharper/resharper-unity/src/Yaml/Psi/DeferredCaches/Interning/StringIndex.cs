using JetBrains.Serialization;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning
{
    public struct StringIndex
    {
        private readonly int myHash;
        
        public StringIndex(string index)
        {
            myHash = index.GetPlatformIndependentHashCode();
        }

        private StringIndex(int hash)
        {
            myHash = hash;
        }

        public static StringIndex Read(UnsafeReader reader)
        {
            return new StringIndex(reader.ReadInt32());
        }

        public static void Write(UnsafeWriter writer, StringIndex value)
        {
            writer.Write(value.myHash);
        }

        public bool Equals(StringIndex other)
        {
            return myHash == other.myHash;
        }

        public override bool Equals(object obj)
        {
            return obj is StringIndex other && Equals(other);
        }

        public override int GetHashCode()
        {
            return myHash;
        }
    }
}