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

        public long OwnerId { get; }

        private static object Read(UnsafeReader reader)
        {
            var ownerId = reader.ReadLong();
            var count = reader.ReadInt32();
            var list = new LocalList<InspectorVariableUsage>();

            for (int i = 0; i < count; i++)
            {
                list.Add(reader.ReadPolymorphic<InspectorVariableUsage>());
            }
            
            var result = new AssetInspectorValuesDataElement(ownerId);
            result.AddData(list);
            return result;
        }

        private static void Write(UnsafeWriter writer, AssetInspectorValuesDataElement value)
        {
            writer.Write(value.OwnerId);
            writer.Write(value.myVariableUsages.Count);
            foreach (var v in value.myVariableUsages)
            {
                writer.WritePolymorphic(v);
            }
        }
        
        private readonly List<InspectorVariableUsage> myVariableUsages = new List<InspectorVariableUsage>();
        public string ContainerId => nameof(AssetInspectorValuesContainer);

        public AssetInspectorValuesDataElement(IPsiSourceFile sourceFile) : this(sourceFile.PsiStorage.PersistentIndex)
        {
        }

        private AssetInspectorValuesDataElement(long ownerId)
        {
            OwnerId = ownerId;
        }
        
        public void AddData(object unityAssetDataElement)
        {
            if (unityAssetDataElement == null)
                return;
            
            var usages = (LocalList<InspectorVariableUsage>) unityAssetDataElement;
            foreach (var variableUsage in usages)
            {
                myVariableUsages.Add(variableUsage);
            }
        }

        public IEnumerable<InspectorVariableUsagePointer> EnumeratePointers()
        {
            for (int i = 0; i < myVariableUsages.Count; i++)
                yield return new InspectorVariableUsagePointer(OwnerId, i);
        }

        public InspectorVariableUsage GetVariableUsage(InspectorVariableUsagePointer pointer)
        {
            return myVariableUsages[pointer.Index];
        }
    }
}