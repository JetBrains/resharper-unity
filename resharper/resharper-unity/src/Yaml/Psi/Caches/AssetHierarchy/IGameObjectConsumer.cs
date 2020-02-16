using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.Elements;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy
{
    public interface IGameObjectConsumer
    {
        bool AddGameObject(GameObjectHierarchy gameObject);
    }
}