using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.Elements
{
    [PolymorphicMarshaller]
    public class TransformHierarchy : ComponentHierarchy
    {
        [UsedImplicitly] 
        public new static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader) => new TransformHierarchy(reader.ReadPolymorphic<LocalReference>(), reader.ReadPolymorphic<IHierarchyReference>(),
            reader.ReadPolymorphic<IHierarchyReference>(), reader.ReadPolymorphic<LocalReference>(),
            reader.ReadPolymorphic<ExternalReference>(), reader.ReadBool());

        [UsedImplicitly]
        public new static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as TransformHierarchy);

        private static void Write(UnsafeWriter writer, TransformHierarchy value)
        {
            writer.WritePolymorphic(value.Location);
            writer.WritePolymorphic(value.GameObjectReference);
            writer.WritePolymorphic(value.Parent);
            writer.WritePolymorphic(value.PrefabInstance);
            writer.WritePolymorphic(value.CorrespondingSourceObject);
            writer.Write(value.IsStripped);
        }
        public IHierarchyReference Parent { get; }

        public TransformHierarchy(LocalReference location, IHierarchyReference gameObjectReference, IHierarchyReference parent,
            LocalReference prefabInstance, ExternalReference correspondingSourceObject, bool isStripped) 
            : base(location, gameObjectReference, prefabInstance, correspondingSourceObject, isStripped)
        {
            Parent = parent;
        }

        protected bool Equals(TransformHierarchy other)
        {
            return Equals(Location, other.Location) && Equals(GameObjectReference, other.GameObjectReference) && Equals(Parent, other.Parent) && IsStripped == other.IsStripped;
        }
    }
}