using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues
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
            writer.Write(value.myVariableUsages.Count);
            foreach (var v in value.myVariableUsages)
            {
                writer.WritePolymorphic(v);
            }
        }
        
        private readonly List<InspectorVariableUsage> myVariableUsages = new List<InspectorVariableUsage>();
        public string ContainerId => nameof(AssetInspectorValuesContainer);

        public AssetInspectorValuesDataElement(IEnumerable<InspectorVariableUsage> usages)
        {
            foreach (var inspectorVariableUsage in usages)
            {
                myVariableUsages.Add(inspectorVariableUsage);
            }    
        }
        
        
        public void AddData(IUnityAssetDataElement unityAssetDataElement)
        {
            var valuesElement = unityAssetDataElement as AssetInspectorValuesDataElement;
            foreach (var variableUsage in valuesElement.myVariableUsages)
            {
                myVariableUsages.Add(variableUsage);
            }
        }

        public IEnumerable<InspectorVariableUsagePointer> EnumeratePointers()
        {
            for (int i = 0; i < myVariableUsages.Count; i++)
                yield return new InspectorVariableUsagePointer(i);
        }

        public InspectorVariableUsage GetVariableUsage(InspectorVariableUsagePointer pointer)
        {
            return myVariableUsages[pointer.Index];
        }
    }
}