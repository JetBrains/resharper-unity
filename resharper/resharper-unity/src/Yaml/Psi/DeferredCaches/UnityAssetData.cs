using System.Collections.Generic;
using JetBrains.Application.PersistentMap;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches
{
    public class UnityAssetData
    {
        public readonly Dictionary<string, IUnityAssetDataElement> UnityAssetDataElements = new Dictionary<string, IUnityAssetDataElement>();
        public static void WriteDelegate(UnsafeWriter writer, UnityAssetData value)
        {
            writer.Write(value.UnityAssetDataElements.Count);
            foreach (var v in value.UnityAssetDataElements.Values)
            {
                writer.WritePolymorphic(v);
            }
        }

        public static UnityAssetData ReadDelegate(UnsafeReader reader)
        {
             var assetData = new UnityAssetData();
            
             var count = reader.ReadInt32();
             for (int i = 0; i < count; i++)
             {
                 var v = reader.ReadPolymorphic<IUnityAssetDataElement>();
                 assetData.AddDataElement(v);
             }

             return assetData;
        }

        public void AddDataElement(IUnityAssetDataElement dataElement)
        {
            if (UnityAssetDataElements.TryGetValue(dataElement.ContainerId, out var element))
            {
                element.AddData(dataElement);
            }
            else
            {
                UnityAssetDataElements[dataElement.ContainerId] = dataElement;
            }
        }
    }
}