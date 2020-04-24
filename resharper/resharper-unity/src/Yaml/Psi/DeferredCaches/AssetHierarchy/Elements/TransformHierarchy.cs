using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    public readonly struct TransformHierarchy : ITransformHierarchy
    {
        public LocalReference Location { get; }
        public LocalReference Owner { get; }
        public LocalReference Parent { get; }
        private readonly int myRootIndex;

        public TransformHierarchy(LocalReference location, LocalReference owner, LocalReference parent, int rootIndex)
        {
            Location = location;
            Owner = owner;
            Parent = parent;
            myRootIndex = rootIndex;
        }

        public IHierarchyElement Import(IPrefabInstanceHierarchy prefabInstanceHierarchy)
        {
            return new ImportedTransformHierarchy(prefabInstanceHierarchy, this);
        }

        public string Name => "Transform";
        public int RootIndex => myRootIndex;

        public static void Write(UnsafeWriter writer, TransformHierarchy transformHierarchy)
        {
            transformHierarchy.Location.WriteTo(writer);
            transformHierarchy.Owner.WriteTo(writer);
            transformHierarchy.Parent.WriteTo(writer);
            writer.Write(transformHierarchy.myRootIndex);
        }

        public static TransformHierarchy Read(UnsafeReader reader)
        {
            return new TransformHierarchy(HierarchyReferenceUtil.ReadLocalReferenceFrom(reader),
                HierarchyReferenceUtil.ReadLocalReferenceFrom(reader),
                HierarchyReferenceUtil.ReadLocalReferenceFrom(reader),
                reader.ReadInt32());
        }
    }
}