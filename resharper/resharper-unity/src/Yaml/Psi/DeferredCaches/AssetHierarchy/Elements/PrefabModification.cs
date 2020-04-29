using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.Serialization;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    [PolymorphicMarshaller]
    public class PrefabModification
    {
        [NotNull]
        public IHierarchyReference Target { get; }
        [NotNull]
        public string PropertyPath { get; }
        
        [CanBeNull]
        public IAssetValue Value { get; }
        
        public TextRange ValueRange { get; }
        
        [CanBeNull]
        public IHierarchyReference ObjectReference { get; }

        [UsedImplicitly] 
        public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader)
        {
            var target = HierarchyReferenceUtil.ReadReferenceFrom(reader);
            var path = reader.ReadString();
            var value = reader.ReadPolymorphic<IAssetValue>();
            var range = new TextRange(reader.ReadInt(), reader.ReadInt());
            IHierarchyReference objectReference = null;
            if (reader.ReadBool())
                objectReference = HierarchyReferenceUtil.ReadReferenceFrom(reader);
            return new PrefabModification(target, path, value, range, objectReference);
        }

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as PrefabModification);

        private static void Write(UnsafeWriter writer, PrefabModification value)
        {
            value.Target.WriteTo(writer);
            writer.Write(value.PropertyPath);
            writer.WritePolymorphic(value.Value);
            writer.Write(value.ValueRange.StartOffset);
            writer.Write(value.ValueRange.EndOffset);
            if (value.ObjectReference == null)
            {
                writer.Write(false);
            }
            else
            {
                writer.Write(true);
                value.ObjectReference.WriteTo(writer);
            }
        }

        public PrefabModification(IHierarchyReference target, string propertyPath, IAssetValue value, TextRange valueRange, IHierarchyReference objectReference)
        {
            Target = target;
            PropertyPath = propertyPath;
            Value = value;
            ValueRange = valueRange;
            ObjectReference = objectReference;
        }
    }
}