using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetInspectorValues
{
    [PolymorphicMarshaller]
    public class AssetInspectorValuesDataElement : IUnityAssetDataElement
    {
        [UsedImplicitly] 
        public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;
        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as AssetInspectorValuesDataElement);


        private static object Read(UnsafeReader reader)
        {
            var count = reader.ReadInt32();
            var list = new List<InspectorVariableUsage>();

            for (int i = 0; i < count; i++)
            {
                list.Add(reader.ReadPolymorphic<InspectorVariableUsage>());
            }
            return new AssetInspectorValuesDataElement(list);
        }

        private static void Write(UnsafeWriter writer, AssetInspectorValuesDataElement value)
        {
            writer.Write(value.VariableUsages.Count);
            foreach (var v in value.VariableUsages)
            {
                writer.WritePolymorphic(v);
            }
        }
        
        public readonly List<InspectorVariableUsage> VariableUsages = new List<InspectorVariableUsage>();
        public string ContainerId => nameof(AssetInspectorValuesContainer);

        public AssetInspectorValuesDataElement(IEnumerable<InspectorVariableUsage> usages)
        {
            foreach (var inspectorVariableUsage in usages)
            {
                VariableUsages.Add(inspectorVariableUsage);
            }    
        }
        
        
        public void AddData(IUnityAssetDataElement unityAssetDataElement)
        {
            var valuesElement = unityAssetDataElement as AssetInspectorValuesDataElement;
            foreach (var variableUsage in valuesElement.VariableUsages)
            {
                VariableUsages.Add(variableUsage);
            }
        }
    }
}