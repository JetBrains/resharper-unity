using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues
{
    [PolymorphicMarshaller]
    public class ComponentHierarchyElement : IUnityHierarchyElement
    {
        public FileID Id { get; }
        public FileID CorrespondingSourceObject { get; }
        public FileID PrefabInstance { get; }
        public FileID GameObject { get; }
        public bool IsStripped { get; }
        
        public ComponentHierarchyElement(FileID id, FileID correspondingSourceObject, FileID prefabInstance, FileID gameObject, bool isStripped)
        {
            Id = id;
            CorrespondingSourceObject = correspondingSourceObject  ?? FileID.Null;
            PrefabInstance = prefabInstance  ?? FileID.Null;
            GameObject = gameObject  ?? FileID.Null;
            IsStripped = isStripped;
        }
        
        [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;
        [UsedImplicitly] public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as ComponentHierarchyElement);

        private static ComponentHierarchyElement Read(UnsafeReader reader)
        {
            return new ComponentHierarchyElement(
                FileID.ReadFrom(reader),
                FileID.ReadFrom(reader),
                FileID.ReadFrom(reader),
                FileID.ReadFrom(reader),
                reader.ReadBool());
        }

        private static void Write(UnsafeWriter writer, ComponentHierarchyElement value)
        {
            value.Id.WriteTo(writer);
            value.CorrespondingSourceObject.WriteTo(writer);
            value.PrefabInstance.WriteTo(writer);
            value.GameObject.WriteTo(writer);
            writer.Write(value.IsStripped);
        }

        protected bool Equals(ComponentHierarchyElement other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ComponentHierarchyElement) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}