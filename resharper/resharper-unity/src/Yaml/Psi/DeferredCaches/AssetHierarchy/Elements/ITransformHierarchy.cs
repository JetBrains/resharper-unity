using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    public interface ITransformHierarchy : IComponentHierarchy
    {
        LocalReference Parent { get; }
        int RootIndex { get; }
    }
}