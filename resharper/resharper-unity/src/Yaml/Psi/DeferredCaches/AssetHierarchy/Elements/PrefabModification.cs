using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    [PolymorphicMarshaller]
    public class PrefabModification
    {
        [NotNull]
        public IHierarchyReference Target { get; }
        [NotNull]
        public string PropertyPath { get; }
        [NotNull]
        public IAssetValue Value { get; }

        [UsedImplicitly] 
        public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader) => new PrefabModification(HierarchyReferenceUtil.ReadReferenceFrom(reader),
            reader.ReadString(), reader.ReadPolymorphic<IAssetValue>());

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as PrefabModification);

        private static void Write(UnsafeWriter writer, PrefabModification value)
        {
            value.Target.WriteTo(writer);
            writer.Write(value.PropertyPath);
            writer.WritePolymorphic(value.Value);
        }

        public PrefabModification(IHierarchyReference target, string propertyPath, IAssetValue value)
        {
            Target = target;
            PropertyPath = propertyPath;
            Value = value;
        }
    }
}