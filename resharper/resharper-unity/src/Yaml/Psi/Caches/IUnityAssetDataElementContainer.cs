using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    public interface IUnityAssetDataElementContainer
    {
        IUnityAssetDataElement Build(Lifetime lifetime, IPsiSourceFile currentSourceFile, AssetDocument assetDocument);
        void Drop(IPsiSourceFile sourceFile, IUnityAssetDataElement unityAssetDataElement);
        void Merge(IPsiSourceFile sourceFile, IUnityAssetDataElement unityAssetDataElement);
        
        string Id { get; }
    }
}