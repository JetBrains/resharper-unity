using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    public interface IGameObjectHierarchy : IHierarchyElement
    {
        string GetName(UnityInterningCache cache);
        ITransformHierarchy GetTransformHierarchy(UnityInterningCache cache, AssetDocumentHierarchyElement owner);
    }
}