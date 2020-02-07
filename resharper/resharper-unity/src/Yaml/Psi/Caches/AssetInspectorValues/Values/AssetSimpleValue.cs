using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Serialization;
using static JetBrains.Serialization.UnsafeWriter;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetInspectorValues.Values
{
    [PolymorphicMarshaller]
    public class AssetSimpleValue : IAssetValue
    {
        [UsedImplicitly] 
        public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader) => new AssetSimpleValue(reader.ReadString());

        [UsedImplicitly]
        public static WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as AssetSimpleValue);

        private static void Write(UnsafeWriter writer, AssetSimpleValue value)
        {
            writer.Write(value.SimpleValue);
        }

        public AssetSimpleValue(string value)
        {
            SimpleValue = value;
        }

        public string SimpleValue { get; }
    }
}