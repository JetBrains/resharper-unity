namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches
{
    public interface IUnityAssetDataElement
    {
        string ContainerId { get; }

        void AddData(IUnityAssetDataElement unityAssetDataElement);
    }
}