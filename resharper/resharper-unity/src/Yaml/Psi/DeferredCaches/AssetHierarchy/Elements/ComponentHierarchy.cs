using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    [PolymorphicMarshaller]
    public class ComponentHierarchy : IHierarchyElement
    {
        [UsedImplicitly] 
        public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader) => new ComponentHierarchy(reader.ReadString(), reader.ReadPolymorphic<LocalReference>(), reader.ReadPolymorphic<IHierarchyReference>(),
            reader.ReadPolymorphic<LocalReference>(), reader.ReadPolymorphic<ExternalReference>(), reader.ReadBool());

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as ComponentHierarchy);

        private static void Write(UnsafeWriter writer, ComponentHierarchy value)
        {
            writer.Write(value.Name);
            writer.WritePolymorphic(value.Location);
            writer.WritePolymorphic(value.GameObjectReference);
            writer.WritePolymorphic(value.PrefabInstance);
            writer.WritePolymorphic(value.CorrespondingSourceObject);
            writer.Write(value.IsStripped);
        }
        
        public ComponentHierarchy(string name, LocalReference localReference, IHierarchyReference gameObject,
            LocalReference prefabInstance, ExternalReference correspondingSourceObject, bool isStripped)
        {
            Name = name;
            Location = localReference;
            GameObjectReference = gameObject;
            PrefabInstance = prefabInstance;
            CorrespondingSourceObject = correspondingSourceObject;
            IsStripped = isStripped;
        }

        public string Name { get; }
        public LocalReference Location { get; }
        public IHierarchyReference GameObjectReference { get; }
        public bool IsStripped { get; }
        public LocalReference PrefabInstance { get; }
        public ExternalReference CorrespondingSourceObject { get; }

        protected bool Equals(ComponentHierarchy other)
        {
            return Equals(Location, other.Location) && Equals(GameObjectReference, other.GameObjectReference) && IsStripped == other.IsStripped;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ComponentHierarchy) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Location != null ? Location.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (GameObjectReference != null ? GameObjectReference.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ IsStripped.GetHashCode();
                return hashCode;
            }
        }
    }
}