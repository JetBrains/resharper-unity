using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetUsages
{
    [PolymorphicMarshaller]
    public class AssetUsagesDataElement : IUnityAssetDataElement
    {
        public readonly List<AssetUsage> AssetUsages = new List<AssetUsage>();
        [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader)
        {
            var count = reader.ReadInt32();
            var result =  new AssetUsagesDataElement();

            for (int i = 0; i < count; i++)
                result.AssetUsages.Add(AssetUsage.ReadFrom(reader));

            return result;
        }

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as AssetUsagesDataElement);

        private static void Write(UnsafeWriter writer, AssetUsagesDataElement value)
        {
            writer.Write(value.AssetUsages.Count);
            foreach (var v in value.AssetUsages)
            {
                v.WriteTo(writer);
            }
        }

        public AssetUsagesDataElement(AssetUsage assetUsage)
        {
            AssetUsages.Add(assetUsage);
        }

        private AssetUsagesDataElement()
        {
            
        }

        public string ContainerId => nameof(AssetUsagesElementContainer);
        public void AddData(IUnityAssetDataElement unityAssetDataElement)
        {
            var element = unityAssetDataElement as AssetUsagesDataElement;
            foreach (var usage in element.AssetUsages)
            {
                AssetUsages.Add(usage);
            }
        }
    }
}