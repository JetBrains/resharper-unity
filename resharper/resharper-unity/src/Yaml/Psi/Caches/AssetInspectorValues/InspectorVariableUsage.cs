using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetInspectorValues.Values;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetInspectorValues
{
    [PolymorphicMarshaller]
    public class InspectorVariableUsage
    {
        [UsedImplicitly] 
        public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader) => new InspectorVariableUsage(reader.ReadPolymorphic<LocalReference>(), reader.ReadPolymorphic<IHierarchyReference>(),
            reader.ReadString(), reader.ReadPolymorphic<IAssetValue>());

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as InspectorVariableUsage);

        private static void Write(UnsafeWriter writer, InspectorVariableUsage value)
        {
            writer.WritePolymorphic(value.Location);
            writer.WritePolymorphic(value.ScriptReference);
            writer.Write(value.Name);
            writer.WritePolymorphic(value.Value);
        }

        public InspectorVariableUsage(LocalReference location, IHierarchyReference scriptReference, string name,
            IAssetValue assetValue)
        {
            Location = location;
            ScriptReference = scriptReference;
            Name = name;
            Value = assetValue;
        }
        
        public LocalReference Location { get; }
        public IHierarchyReference ScriptReference { get; }
        public string Name { get; }
        public IAssetValue Value { get; }
    }
}