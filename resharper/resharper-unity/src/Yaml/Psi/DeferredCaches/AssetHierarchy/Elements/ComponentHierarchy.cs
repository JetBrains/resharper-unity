using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    [PolymorphicMarshaller]
    public class ComponentHierarchy : IComponentHierarchy
    {
        [UsedImplicitly] 
        public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader) => new ComponentHierarchy(reader.ReadString(), reader.ReadPolymorphic<LocalReference>(), reader.ReadPolymorphic<LocalReference>(),
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
        
        public ComponentHierarchy(string name, LocalReference localReference, LocalReference gameObject,
            LocalReference prefabInstance, ExternalReference correspondingSourceObject, bool isStripped)
        {
            Name = name;
            Location = localReference;
            GameObjectReference = gameObject;
            PrefabInstance = prefabInstance;
            CorrespondingSourceObject = correspondingSourceObject;
            IsStripped = isStripped;
        }

        public virtual string Name { get; }
        public virtual LocalReference Location { get; }
        public virtual LocalReference GameObjectReference { get; }
        public virtual bool IsStripped { get; }
        public virtual LocalReference PrefabInstance { get; }
        public virtual ExternalReference CorrespondingSourceObject { get; }
        public virtual IHierarchyElement Import(IPrefabInstanceHierarchy prefabInstanceHierarchy)
        {
            return new ImportedComponentHierarchy(prefabInstanceHierarchy, this);
        }

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
                var hashCode = Location.GetHashCode();
                hashCode = (hashCode * 397) ^ (GameObjectReference != null ? GameObjectReference.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ IsStripped.GetHashCode();
                return hashCode;
            }
        }
    }
}