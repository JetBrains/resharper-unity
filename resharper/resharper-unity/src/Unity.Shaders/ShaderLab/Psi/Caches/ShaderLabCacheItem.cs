#nullable enable
using JetBrains.Serialization;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches
{
    public record ShaderLabCacheItem(string Name, int DeclarationOffset)
    {
        public static readonly IUnsafeMarshaller<ShaderLabCacheItem> Marshaller = new UniversalMarshaller<ShaderLabCacheItem>(Read, Write);
        
        private static ShaderLabCacheItem Read(UnsafeReader reader)
        {
            var name = reader.ReadString()!;
            var offset = reader.ReadInt();
            return new ShaderLabCacheItem(name, offset);
        }

        private static void Write(UnsafeWriter writer, ShaderLabCacheItem value)
        {
            writer.WriteString(value.Name);
            writer.WriteInt32(value.DeclarationOffset);
        }
    }
}