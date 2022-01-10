namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    public interface IGameObjectHierarchy : IHierarchyElement
    {
        string Name { get; }
        ITransformHierarchy GetTransformHierarchy(AssetDocumentHierarchyElement owner);
    }
}