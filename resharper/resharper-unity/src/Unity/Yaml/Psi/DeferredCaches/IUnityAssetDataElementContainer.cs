using JetBrains.Annotations;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches
{
    [DerivedComponentsInstantiationRequirement(InstantiationRequirement.DeadlockSafe)]
    public interface IUnityAssetDataElementContainer
    {
        [NotNull]
        string Id { get; }

        int Order { get; }

        [NotNull]
        IUnityAssetDataElement CreateDataElement([NotNull] IPsiSourceFile sourceFile);

        bool IsApplicable([NotNull] IPsiSourceFile currentAssetSourceFile);
        
        [CanBeNull]
        object Build([NotNull] IPsiSourceFile currentAssetSourceFile,
                     [NotNull] AssetDocument assetDocument);

        void Drop([NotNull] IPsiSourceFile currentAssetSourceFile,
                  [NotNull] AssetDocumentHierarchyElement assetDocumentHierarchyElement,
                  [NotNull] IUnityAssetDataElement unityAssetDataElement);

        void Merge([NotNull] IPsiSourceFile currentAssetSourceFile,
                   [NotNull] AssetDocumentHierarchyElement assetDocumentHierarchyElement,
                   [NotNull] IUnityAssetDataElementPointer unityAssetsCache,
                   [NotNull] IUnityAssetDataElement unityAssetDataElement);

        void Invalidate();
    }
}