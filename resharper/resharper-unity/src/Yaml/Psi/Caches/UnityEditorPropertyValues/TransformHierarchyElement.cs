using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues
{
    [PolymorphicMarshaller]
    public class TransformHierarchyElement : ComponentHierarchyElement
    {
        
        public int RootOrder { get; }
        public FileID Father { get; }
        public TransformHierarchyElement(FileID id, FileID correspondingSourceObject, FileID prefabParentObject, bool isStripped, int rootOrder, FileID gameObject, FileID father)
            : base(id, correspondingSourceObject, prefabParentObject, gameObject, isStripped)
        {
            RootOrder = rootOrder;
            Father = father ?? FileID.Null;
        }
        
        [UsedImplicitly] public new static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;
        [UsedImplicitly] public new static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as TransformHierarchyElement);


        private static TransformHierarchyElement Read(UnsafeReader reader)
        {
            return new TransformHierarchyElement(
                FileID.ReadFrom(reader),
                FileID.ReadFrom(reader),
                FileID.ReadFrom(reader),
                reader.ReadBool(),
                reader.ReadInt32(),
                FileID.ReadFrom(reader),
                FileID.ReadFrom(reader));
        }

        private static void Write(UnsafeWriter writer, TransformHierarchyElement value)
        {
            value.Id.WriteTo(writer);
            value.CorrespondingSourceObject.WriteTo(writer);
            value.PrefabInstance.WriteTo(writer);
            writer.Write(value.IsStripped);
            writer.Write(value.RootOrder);
            value.GameObject.WriteTo(writer);
            value.Father.WriteTo(writer);
        }

        protected bool Equals(TransformHierarchyElement other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TransformHierarchyElement) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}