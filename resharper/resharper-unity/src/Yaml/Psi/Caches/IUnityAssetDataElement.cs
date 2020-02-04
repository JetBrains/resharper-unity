using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    public interface IUnityAssetDataElement
    {
        string ContainerId { get; }

        void AddData(IUnityAssetDataElement unityAssetDataElement);
    }
}