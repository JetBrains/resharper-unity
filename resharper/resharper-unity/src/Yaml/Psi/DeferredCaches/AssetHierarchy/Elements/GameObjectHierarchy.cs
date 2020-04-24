using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    public readonly struct GameObjectHierarchy : IGameObjectHierarchy
    {
        public LocalReference Location { get; }
        public string Name { get; }

        public GameObjectHierarchy(LocalReference location, string name)
        {
            Location = location;
            Name = name;
        }

        public IHierarchyElement Import(IPrefabInstanceHierarchy prefabInstanceHierarchy)
        {
            return new ImportedGameObjectHierarchy(prefabInstanceHierarchy, this);
        }
        public ITransformHierarchy GetTransformHierarchy(AssetDocumentHierarchyElement owner)
        {
            return owner.GetTransformHierarchy( this);
        }

        public static void Write(UnsafeWriter writer, GameObjectHierarchy gameObjectHierarchy)
        {
            gameObjectHierarchy.Location.WriteTo(writer);
            writer.Write(gameObjectHierarchy.Name);
        }

        public static GameObjectHierarchy Read(UnsafeReader reader)
        {
            return new GameObjectHierarchy(HierarchyReferenceUtil.ReadLocalReferenceFrom(reader), reader.ReadString());
        }
    }
}