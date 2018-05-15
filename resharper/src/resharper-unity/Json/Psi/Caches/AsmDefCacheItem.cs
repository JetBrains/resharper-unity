using System.Collections.Generic;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Json.Psi.Caches
{
    public class AsmDefCacheItem
    {
        public static readonly IUnsafeMarshaller<AsmDefCacheItem> Marshaller =
            new UniversalMarshaller<AsmDefCacheItem>(Read, Write);

        private readonly string[] myReferences;

        public AsmDefCacheItem(string name, int declarationOffset, string[] references)
        {
            Name = name;
            DeclarationOffset = declarationOffset;
            myReferences = references;
        }

        public string Name { get; }
        public int DeclarationOffset { get; }
        public IEnumerable<string> References => myReferences;

        private static AsmDefCacheItem Read(UnsafeReader reader)
        {
            var name = reader.ReadString();
            var declarationOffset = reader.ReadInt();
            var references = UnsafeMarshallers.StringArrayMarshaller.Unmarshal(reader);
            return new AsmDefCacheItem(name, declarationOffset, references);
        }

        private static void Write(UnsafeWriter writer, AsmDefCacheItem value)
        {
            writer.Write(value.Name);
            writer.Write(value.DeclarationOffset);
            UnsafeMarshallers.StringArrayMarshaller.Marshal(writer, value.myReferences);
        }
    }
}