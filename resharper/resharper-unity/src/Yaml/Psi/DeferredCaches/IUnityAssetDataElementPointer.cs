using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches
{
    public interface IUnityAssetDataElementPointer
    {
        IUnityAssetDataElement GetElement(IPsiSourceFile assetSourceFile, string containerId);
    }

}