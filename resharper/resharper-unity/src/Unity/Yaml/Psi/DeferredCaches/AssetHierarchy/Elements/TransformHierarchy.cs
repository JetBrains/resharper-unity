using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    public readonly struct TransformHierarchy : ITransformHierarchy
    {
        public LocalReference Location { get; }
        public LocalReference OwningGameObject { get; }
        public LocalReference ParentTransform { get; }
        private readonly int myRootIndex;

        public TransformHierarchy(LocalReference location, LocalReference owner, LocalReference parent, int rootIndex)
        {
            Location = location;
            OwningGameObject = owner;
            ParentTransform = parent;
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
            transformHierarchy.OwningGameObject.WriteTo(writer);
            transformHierarchy.ParentTransform.WriteTo(writer);
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