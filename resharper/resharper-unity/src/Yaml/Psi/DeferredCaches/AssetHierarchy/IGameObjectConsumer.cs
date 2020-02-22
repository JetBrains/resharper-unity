using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy
{
    public interface IGameObjectConsumer
    {
        bool AddGameObject(AssetDocumentHierarchyElement owner, IGameObjectHierarchy gameObject);
    }
}