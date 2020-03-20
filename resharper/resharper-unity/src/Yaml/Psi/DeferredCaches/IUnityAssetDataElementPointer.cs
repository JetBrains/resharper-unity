using System;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches
{
    public interface IUnityAssetDataElementPointer
    {
        IUnityAssetDataElement Element { get; }
    }

    public class UnityAssetDataElementPointer : IUnityAssetDataElementPointer
    {
        private readonly Func<IUnityAssetDataElement> myDataElementProvider;

        public UnityAssetDataElementPointer(Func<IUnityAssetDataElement> dataElementProvider)
        {
            myDataElementProvider = dataElementProvider;
        }

        public IUnityAssetDataElement Element => myDataElementProvider();
    }
}