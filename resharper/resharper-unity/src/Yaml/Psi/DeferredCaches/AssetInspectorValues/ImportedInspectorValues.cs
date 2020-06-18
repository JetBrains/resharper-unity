using System.Collections.Generic;
using JetBrains.Application.PersistentMap;
using JetBrains.Collections;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues
{
    public class ImportedInspectorValues
    {
        public Dictionary<ImportedValueReference, (IAssetValue value, AssetReferenceValue objectReference)> Modifications = new Dictionary<ImportedValueReference, (IAssetValue value, AssetReferenceValue objectReference)>();

        public void WriteTo(UnsafeWriter writer)
        {
            writer.Write(Modifications.Count);
            foreach (var (reference, value) in Modifications)
            {
                reference.WriteTo(writer);
                writer.WritePolymorphic(value.value);
                writer.WritePolymorphic(value.objectReference);
            }
        }

        public static ImportedInspectorValues ReadFrom(UnsafeReader reader)
        {
            var count = reader.ReadInt();
            var result = new ImportedInspectorValues();
            for (int i = 0; i < count; i++)
            {
                result.Modifications[ImportedValueReference.ReadFrom(reader)] = (reader.ReadPolymorphic<IAssetValue>() as AssetSimpleValue, reader.ReadPolymorphic<IAssetValue>() as AssetReferenceValue);
            }
            
            return result;
        }
    }
}