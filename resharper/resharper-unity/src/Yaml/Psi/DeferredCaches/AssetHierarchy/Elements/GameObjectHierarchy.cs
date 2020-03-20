using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    public struct GameObjectHierarchy : IGameObjectHierarchy
    {
        private readonly ReferenceIndex myLocation;
        private readonly StringIndex myName;

        public GameObjectHierarchy(ReferenceIndex location, StringIndex name)
        {
            myLocation = location;
            myName = name;
        }

        public LocalReference GetLocation(UnityInterningCache cache) => cache.GetReference<LocalReference>(myLocation);

        public IHierarchyElement Import(UnityInterningCache cache, IPrefabInstanceHierarchy prefabInstanceHierarchy)
        {
            return new ImportedGameObjectHierarchy(prefabInstanceHierarchy, this);
        }

        public string GetName(UnityInterningCache cache) => cache.GetString(myName);

        public ITransformHierarchy GetTransformHierarchy(UnityInterningCache cache, AssetDocumentHierarchyElement owner)
        {
            return owner.GetTransformHierarchy(cache, this);
        }

        public static void Write(UnsafeWriter writer, GameObjectHierarchy gameObjectHierarchy)
        {
            ReferenceIndex.Write(writer, gameObjectHierarchy.myLocation);
            StringIndex.Write(writer, gameObjectHierarchy.myName);
        }

        public static GameObjectHierarchy Read(UnsafeReader reader)
        {
            return new GameObjectHierarchy(
                ReferenceIndex.Read(reader),
                StringIndex.Read(reader));
        }
    }
}