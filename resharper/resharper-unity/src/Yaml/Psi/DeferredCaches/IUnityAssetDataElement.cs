namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches
{
    public interface IUnityAssetDataElement
    {
        long OwnerId { get; }
        
        string ContainerId { get; }

        void AddData(object data);
    }
}