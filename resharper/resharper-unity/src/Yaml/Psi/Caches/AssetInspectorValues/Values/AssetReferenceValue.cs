using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetInspectorValues.Values
{
    [PolymorphicMarshaller]
    public class AssetReferenceValue : IAssetValue
    {
        [UsedImplicitly] 
        public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader) => new AssetReferenceValue( reader.ReadPolymorphic<IHierarchyReference>());

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as AssetReferenceValue);

        private static void Write(UnsafeWriter writer, AssetReferenceValue value)
        {
            writer.WritePolymorphic(value.Reference);
        }
        
        public IHierarchyReference Reference { get; }
        
        public AssetReferenceValue(IHierarchyReference reference)
        {
            Reference = reference;
        }
    }
}