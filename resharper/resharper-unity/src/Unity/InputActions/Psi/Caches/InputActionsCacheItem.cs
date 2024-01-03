using System.Collections.Generic;
using JetBrains.Serialization;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.Caches
{
    public class InputActionsCacheItem
    {
         private static readonly IUnsafeMarshaller<InputActionsCacheItem> ourInputActionCacheItemMarshaller =
            new UniversalMarshaller<InputActionsCacheItem>(Read, Write);
         
         public static readonly UnsafeFilteredCollectionMarshaller<InputActionsCacheItem, List<InputActionsCacheItem>> Marshaller =
             new(ourInputActionCacheItemMarshaller, n => new List<InputActionsCacheItem>(n), item => item != null);


        public InputActionsCacheItem(string name, int declarationOffset)
        {
            Name = name;
            DeclarationOffset = declarationOffset;
        }

        public string Name { get; }
        public int DeclarationOffset { get; }

        private static InputActionsCacheItem Read(UnsafeReader reader)
        {
            var name = reader.ReadString()!;
            var declarationOffset = reader.ReadInt();
            return new InputActionsCacheItem(name, declarationOffset);
        }

        private static void Write(UnsafeWriter writer, InputActionsCacheItem value)
        {
            writer.Write(value.Name);
            writer.Write(value.DeclarationOffset);
        }
        
        public override string ToString()
        {
            return $"{Name}:{DeclarationOffset}";
        }
    }
}