using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    public readonly struct TransformHierarchy : ITransformHierarchy
    {
        public LocalReference Location { get; }
        public LocalReference OwningGameObject { get; }
        public LocalReference ParentTransform { get; }
        public LocalList<long> Children { get; }
        private readonly int myRootOrder;

        public TransformHierarchy(LocalReference location, LocalReference owner, LocalReference parent, int rootOrder, LocalList<long> children)
        {
            Location = location;
            OwningGameObject = owner;
            ParentTransform = parent;
            myRootOrder = rootOrder;
            Children = children;
        }

        public IHierarchyElement Import(IPrefabInstanceHierarchy prefabInstanceHierarchy)
        {
            return new ImportedTransformHierarchy(prefabInstanceHierarchy, this);
        }

        public string Name => "Transform";
        public int RootOrder => myRootOrder;

        public static void Write(UnsafeWriter writer, TransformHierarchy transformHierarchy)
        {
            transformHierarchy.Location.WriteTo(writer);
            transformHierarchy.OwningGameObject.WriteTo(writer);
            transformHierarchy.ParentTransform.WriteTo(writer);
            writer.Write(transformHierarchy.myRootOrder);
            writer.Write(transformHierarchy.Children.Count);
            foreach (var child in transformHierarchy.Children) writer.Write(child);
        }

        public static TransformHierarchy Read(UnsafeReader reader)
        {
            var location = HierarchyReferenceUtil.ReadLocalReferenceFrom(reader);
            var owningGameObject = HierarchyReferenceUtil.ReadLocalReferenceFrom(reader);
            var parentTransform = HierarchyReferenceUtil.ReadLocalReferenceFrom(reader);
            var rootOrder = reader.ReadInt32();
            var count = reader.ReadInt32();
            var children = new LocalList<long>(count);
            for (var i = 0; i < count; i++)
                children.Add(reader.ReadLong());

            return new TransformHierarchy(location, owningGameObject, parentTransform, rootOrder, children);
        }
    }
}