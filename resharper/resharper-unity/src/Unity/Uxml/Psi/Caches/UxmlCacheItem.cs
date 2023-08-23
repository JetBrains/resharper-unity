using System.Collections.Generic;
using JetBrains.Serialization;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Uxml.Psi.Caches
{
    public class UxmlCacheItem
    {
         private static readonly IUnsafeMarshaller<UxmlCacheItem> ourUxmlCacheItemMarshaller =
            new UniversalMarshaller<UxmlCacheItem>(Read, Write);
         
         public static readonly UnsafeFilteredCollectionMarshaller<UxmlCacheItem, List<UxmlCacheItem>> Marshaller =
             new(ourUxmlCacheItemMarshaller, n => new List<UxmlCacheItem>(n), item => item != null);


        public UxmlCacheItem(string controlTypeName, string name, int declarationOffset)
        {
            ControlTypeName = controlTypeName;
            Name = name;
            DeclarationOffset = declarationOffset;
        }

        public string ControlTypeName { get; }
        public string Name { get; }
        public int DeclarationOffset { get; }

        private static UxmlCacheItem Read(UnsafeReader reader)
        {
            var controlTypeName = reader.ReadString();
            var name = reader.ReadString()!;
            var declarationOffset = reader.ReadInt();
            return new UxmlCacheItem(controlTypeName, name, declarationOffset);
        }

        private static void Write(UnsafeWriter writer, UxmlCacheItem value)
        {
            writer.Write(value.ControlTypeName);
            writer.Write(value.Name);
            writer.Write(value.DeclarationOffset);
        }
        
        public override string ToString()
        {
            return $"{ControlTypeName}:{Name}:{DeclarationOffset}";
        }
    }
}