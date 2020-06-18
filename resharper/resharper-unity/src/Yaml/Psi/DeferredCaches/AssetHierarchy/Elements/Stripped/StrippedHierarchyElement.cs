using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Stripped
{
    public readonly struct StrippedHierarchyElement : IStrippedHierarchyElement
    {
        public LocalReference Location { get; }
        public LocalReference PrefabInstance { get; }
        public ExternalReference CorrespondingSourceObject { get; }

        public StrippedHierarchyElement(LocalReference location, LocalReference prefabInstance, ExternalReference correspondingSourceObject)
        {
            Location = location;
            PrefabInstance = prefabInstance;
            CorrespondingSourceObject = correspondingSourceObject;
        }


        public IHierarchyElement Import(IPrefabInstanceHierarchy prefabInstanceHierarchy) => null;


        public static void Write(UnsafeWriter writer, StrippedHierarchyElement strippedHierarchyElement)
        {
           strippedHierarchyElement.Location.WriteTo(writer);
           strippedHierarchyElement.PrefabInstance.WriteTo(writer);
           strippedHierarchyElement.CorrespondingSourceObject.WriteTo(writer);
        }

        public static StrippedHierarchyElement Read(UnsafeReader reader)
        {
            return new StrippedHierarchyElement( HierarchyReferenceUtil.ReadLocalReferenceFrom(reader),
                HierarchyReferenceUtil.ReadLocalReferenceFrom(reader), 
                HierarchyReferenceUtil.ReadExternalReferenceFrom(reader));
        }
    }
}