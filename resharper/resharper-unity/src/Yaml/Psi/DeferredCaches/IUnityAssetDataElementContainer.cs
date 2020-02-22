using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches
{
    public interface IUnityAssetDataElementContainer
    {
        IUnityAssetDataElement Build(Lifetime lifetime, IPsiSourceFile currentSourceFile, AssetDocument assetDocument);
        void Drop(IPsiSourceFile sourceFile, IUnityAssetDataElement unityAssetDataElement);
        void Merge(IPsiSourceFile sourceFile, IUnityAssetDataElement unityAssetDataElement);
        
        string Id { get; }
        
        int Order { get; }
        void Invalidate();
    }
}