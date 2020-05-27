using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Psi;
using JetBrains.Serialization;
using JetBrains.Util;

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
            var list = new List<InspectorVariableUsage>(count);

            for (int i = 0; i < count; i++)
            {
                list.Add(reader.ReadPolymorphic<InspectorVariableUsage>());
            }

            var importedInspectorValues = ImportedInspectorValues.ReadFrom(reader);
            var result = new AssetInspectorValuesDataElement(list, importedInspectorValues);
            return result;
        }

        private static void Write(UnsafeWriter writer, AssetInspectorValuesDataElement value)
        {
            writer.Write(value.myVariableUsages.Count);
            foreach (var v in value.myVariableUsages)
            {
                writer.WritePolymorphic(v);
            }

            value.ImportedInspectorValues.WriteTo(writer);
        }

        public IReadOnlyList<InspectorVariableUsage> VariableUsages => myVariableUsages;

        public readonly ImportedInspectorValues ImportedInspectorValues;
        private readonly List<InspectorVariableUsage> myVariableUsages;

        public string ContainerId => nameof(AssetInspectorValuesContainer);

        public AssetInspectorValuesDataElement() : this(new List<InspectorVariableUsage>(),  new ImportedInspectorValues())
        {
        }

        private AssetInspectorValuesDataElement(List<InspectorVariableUsage> inspectorValues,
            ImportedInspectorValues importedInspectorValues)
        {
            ImportedInspectorValues = importedInspectorValues;
            myVariableUsages = inspectorValues;
        }
        
        public void AddData(object unityAssetDataElement)
        {
            if (unityAssetDataElement == null)
                return;
            
            var buildResult = (InspectorValuesBuildResult) unityAssetDataElement;
            foreach (var variableUsage in buildResult.InspectorValues)
            {
                myVariableUsages.Add(variableUsage);
            }

            foreach (var modification in buildResult.ImportedInspectorValues.Modifications)
            {
                ImportedInspectorValues.Modifications[modification.Key] = modification.Value;
            }
        }
    }
}