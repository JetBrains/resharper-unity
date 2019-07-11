using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Swa
{
    [PolymorphicMarshaller]
    public class TransformHierarchyElement : IUnityHierarchyElement
    {
        
        public FileID Id { get; }
        public FileID CorrespondingSourceObject { get; }
        public FileID PrefabInstance { get; }
        public bool IsStripped { get; }
        public int RootOrder { get; }
        public FileID GameObject { get; }
        public FileID Father { get; }
        public IList<FileID> Children { get; }
        
        public TransformHierarchyElement(FileID id, FileID correspondingSourceObject, FileID prefabParentObject, bool isStripped, int rootOrder, FileID gameObject, FileID father, IList<FileID> children)
        {
            Id = id;
            CorrespondingSourceObject = correspondingSourceObject  ?? FileID.Null;
            PrefabInstance = prefabParentObject  ?? FileID.Null;
            IsStripped = isStripped;
            RootOrder = rootOrder;
            GameObject = gameObject  ?? FileID.Null;
            Father = father ?? FileID.Null;
            Children = children;
        }
        
        [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;
        [UsedImplicitly] public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as TransformHierarchyElement);


        private static TransformHierarchyElement Read(UnsafeReader reader)
        {
            return new TransformHierarchyElement(
                FileID.ReadFrom(reader),
                FileID.ReadFrom(reader),
                FileID.ReadFrom(reader),
                reader.ReadBool(),
                reader.ReadInt32(),
                FileID.ReadFrom(reader),
                FileID.ReadFrom(reader),
                ReadChildren(reader));
        }

        private static IList<FileID> ReadChildren(UnsafeReader reader)
        {
            var count = reader.ReadInt32();
            var result = new List<FileID>(count);
            for (var i = 0; i < count; i++) 
                result.Add(FileID.ReadFrom(reader));

            return result;
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
            
            writer.Write(value.Children.Count);
            foreach (var component in value.Children)
            {
                component.WriteTo(writer);
            }
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