using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    [PolymorphicMarshaller]
    public class GameObjectHierarchy : IHierarchyElement
    {
        [UsedImplicitly] 
        public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader) => new GameObjectHierarchy(reader.ReadPolymorphic<LocalReference>(), reader.ReadString(),
            reader.ReadPolymorphic<LocalReference>(), reader.ReadPolymorphic<ExternalReference>(), reader.ReadBool());

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as GameObjectHierarchy);

        private static void Write(UnsafeWriter writer, GameObjectHierarchy value)
        {
            writer.WritePolymorphic(value.Location);
            writer.Write(value.Name);
            writer.WritePolymorphic(value.PrefabInstance);
            writer.WritePolymorphic(value.CorrespondingSourceObject);
            writer.Write(value.IsStripped);
        }
        
        public LocalReference Location { get; }
        public IHierarchyReference GameObjectReference => null;
        public bool IsStripped { get; }
        public LocalReference PrefabInstance { get; }
        public ExternalReference CorrespondingSourceObject { get; }
        public TransformHierarchy Transform { get; internal set; }
        public string Name { get; }

        public GameObjectHierarchy(LocalReference location, string name, LocalReference prefabInstance, ExternalReference correspondingSourceObject, bool isStripped)
        {
            Location = location;
            Name = name;
            PrefabInstance = prefabInstance;
            CorrespondingSourceObject = correspondingSourceObject;
            IsStripped = isStripped;
        }

        protected bool Equals(GameObjectHierarchy other)
        {
            return Location.Equals(other.Location) && IsStripped == other.IsStripped;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((GameObjectHierarchy) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Location.GetHashCode() * 397) ^ IsStripped.GetHashCode();
            }
        }
    }
}