using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches
{
    public interface IUnityAssetDataElementContainer
    {
        IUnityAssetDataElement CreateDataElement(IPsiSourceFile sourceFile);
        object Build(SeldomInterruptChecker checker, IPsiSourceFile currentAssetSourceFile, AssetDocument assetDocument);
        void Drop(IPsiSourceFile currentAssetSourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElement unityAssetDataElement);
        void Merge(IPsiSourceFile currentAssetSourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElementPointer unityAssetsCache, IUnityAssetDataElement unityAssetDataElement);
        
        string Id { get; }
        
        int Order { get; }
        void Invalidate();
    }
}