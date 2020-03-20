using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy
{
    public interface IGameObjectConsumer
    {
        bool AddGameObject(AssetDocumentHierarchyElement owner, UnityInterningCache cache, IGameObjectHierarchy gameObject);
    }
}