using System.Collections.Generic;
using JetBrains.Serialization;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.Caches
{
    public class InputActionCacheItem
    {
         private static readonly IUnsafeMarshaller<InputActionCacheItem> ourInputActionCacheItemMarshaller =
            new UniversalMarshaller<InputActionCacheItem>(Read, Write);
         
         public static readonly UnsafeFilteredCollectionMarshaller<InputActionCacheItem, List<InputActionCacheItem>> Marshaller =
             new(ourInputActionCacheItemMarshaller, n => new List<InputActionCacheItem>(n), item => item != null);


        public InputActionCacheItem(string name, int declarationOffset)
        {
            Name = name;
            DeclarationOffset = declarationOffset;
        }

        public string Name { get; }
        public int DeclarationOffset { get; }

        private static InputActionCacheItem Read(UnsafeReader reader)
        {
            var name = reader.ReadString()!;
            var declarationOffset = reader.ReadInt();
            return new InputActionCacheItem(name, declarationOffset);
        }

        private static void Write(UnsafeWriter writer, InputActionCacheItem value)
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