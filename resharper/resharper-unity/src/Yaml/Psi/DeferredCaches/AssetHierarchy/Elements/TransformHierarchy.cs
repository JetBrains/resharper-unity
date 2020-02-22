using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    [PolymorphicMarshaller]
    public class TransformHierarchy : ComponentHierarchy, ITransformHierarchy
    {
        [UsedImplicitly] 
        public new static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader) => new TransformHierarchy(reader.ReadPolymorphic<LocalReference>(), reader.ReadPolymorphic<LocalReference>(),
            reader.ReadPolymorphic<LocalReference>(), reader.ReadInt32(), reader.ReadPolymorphic<LocalReference>(),
            reader.ReadPolymorphic<ExternalReference>(), reader.ReadBool());

        [UsedImplicitly]
        public new static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as TransformHierarchy);

        private static void Write(UnsafeWriter writer, TransformHierarchy value)
        {
            writer.WritePolymorphic(value.Location);
            writer.WritePolymorphic(value.GameObjectReference);
            writer.WritePolymorphic(value.Parent);
            writer.Write(value.RootIndex);
            writer.WritePolymorphic(value.PrefabInstance);
            writer.WritePolymorphic(value.CorrespondingSourceObject);
            writer.Write(value.IsStripped);
        }
        public virtual LocalReference Parent { get; }
        public virtual int RootIndex { get; }

        public TransformHierarchy(LocalReference location, LocalReference gameObjectReference, LocalReference parent,
            int rootIndex, LocalReference prefabInstance, ExternalReference correspondingSourceObject, bool isStripped) 
            : base("Transform", location, gameObjectReference, prefabInstance, correspondingSourceObject, isStripped)
        {
            Parent = parent;
            RootIndex = rootIndex;
        }

        protected bool Equals(TransformHierarchy other)
        {
            return base.Equals(other) && Equals(Parent, other.Parent) && RootIndex == other.RootIndex;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TransformHierarchy) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (Parent != null ? Parent.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ RootIndex;
                return hashCode;
            }
        }
    }
}