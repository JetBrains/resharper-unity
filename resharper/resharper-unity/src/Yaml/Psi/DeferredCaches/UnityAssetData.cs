using System.Collections.Generic;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Psi;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches
{
    public class UnityAssetData
    {
        public readonly Dictionary<string, IUnityAssetDataElement> UnityAssetDataElements = new Dictionary<string, IUnityAssetDataElement>();

        private UnityAssetData()
        {
            
        }
        
        public UnityAssetData(IPsiSourceFile sourceFile, IEnumerable<IUnityAssetDataElementContainer> containers)
        {
            foreach (var container in containers)
            {
                UnityAssetDataElements[container.Id] = container.CreateDataElement(sourceFile);
            }
        }

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
                 assetData.UnityAssetDataElements[v.ContainerId] = v;

             }

             return assetData;
        }
    }
}