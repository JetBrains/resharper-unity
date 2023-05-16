using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    public interface ITransformHierarchy : IComponentHierarchy
    {
        // TODO : think about store only parent anchor, because file id is stored in Location
        LocalReference ParentTransform { get; }
        int RootOrder { get; }
    }
}