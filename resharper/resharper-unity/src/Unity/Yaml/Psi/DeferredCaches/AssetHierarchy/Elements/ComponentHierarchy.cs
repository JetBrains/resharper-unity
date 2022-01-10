using System;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    public readonly struct ComponentHierarchy : IComponentHierarchy
    {
        public LocalReference Location { get; }
        public LocalReference OwningGameObject { get; }
        public string Name { get; }

        public ComponentHierarchy(LocalReference location, LocalReference owningGameObject, string name)
        {
            Location = location;
            OwningGameObject = owningGameObject;
            Name = String.Intern(name);
        }

        public IHierarchyElement Import(IPrefabInstanceHierarchy prefabInstanceHierarchy)
        {
            return new ImportedComponentHierarchy(prefabInstanceHierarchy, this);
        }

        public static void Write(UnsafeWriter writer, ComponentHierarchy componentHierarchy)
        {
            componentHierarchy.Location.WriteTo(writer);
            componentHierarchy.OwningGameObject.WriteTo(writer);
            writer.Write(componentHierarchy.Name);
        }

        public static ComponentHierarchy Read(UnsafeReader reader)
        {
            return new ComponentHierarchy(HierarchyReferenceUtil.ReadLocalReferenceFrom(reader), 
                HierarchyReferenceUtil.ReadLocalReferenceFrom(reader), reader.ReadString());
        }
    }
}