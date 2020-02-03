using System.Collections.Generic;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Psi;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    public class UnityAssetData
    {
        private Dictionary<string, IUnityAssetDataElement> myUnityAssetDataElements = new Dictionary<string, IUnityAssetDataElement>();
        public static void WriteDelegate(UnsafeWriter writer, UnityAssetData value)
        {
            writer.Write(value.myUnityAssetDataElements.Count);
            foreach (var v in value.myUnityAssetDataElements.Values)
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
            if (myUnityAssetDataElements.TryGetValue(dataElement.ContainerId, out var element))
            {
                element.AddData(dataElement);
            }
            else
            {
                myUnityAssetDataElements[dataElement.ContainerId] = dataElement;
            }
        }

        public void Restore(IPsiSourceFile owner)
        {
            foreach (var unityAssetDataElement in myUnityAssetDataElements.Values)
            {
                unityAssetDataElement.Restoree(owner);
            }
        }
    }
}