using System.Collections.Generic;
using JetBrains.Application.PersistentMap;
using JetBrains.Collections;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents
{
    public class ImportedUnityEventData
    {
        // stipped object location (script with UnityEvent after import), Unity event name, index, fieldName -> value
        public Dictionary<ImportedAssetMethodReference, Dictionary<string, IAssetValue>> ReferenceToImportedData { get; } =
            new Dictionary<ImportedAssetMethodReference, Dictionary<string, IAssetValue>>();

        public void WriteTo(UnsafeWriter writer)
        {
            writer.Write(ReferenceToImportedData.Count);
            foreach (var valueTuple in ReferenceToImportedData)
            {
                valueTuple.Key.WriteTo(writer);
                writer.Write(valueTuple.Value.Count);
                foreach (var (fieldName, value) in valueTuple.Value)
                {
                    writer.Write(fieldName);
                    writer.WritePolymorphic(value);
                }
            }
        }

        public static ImportedUnityEventData ReadFrom(UnsafeReader unsafeReader)
        {
            var result = new ImportedUnityEventData();
            var count = unsafeReader.ReadInt();
            for (int i = 0; i < count; i++)
            {
                var reference = ImportedAssetMethodReference.ReadFrom(unsafeReader);
                var valuesCount = unsafeReader.ReadInt();
                var values = new Dictionary<string, IAssetValue>();
                for (int j = 0; j < valuesCount; j++)
                {
                    values[unsafeReader.ReadString()] = unsafeReader.ReadPolymorphic<IAssetValue>();
                }

                result.ReferenceToImportedData[reference] = values;
            }
            
            
            return result;
        }
    }

    public readonly struct ImportedAssetMethodReference
    {
        public ImportedAssetMethodReference(LocalReference location, string unityEventName, int methodIndex)
        {
            Location = location;
            UnityEventName = unityEventName;
            MethodIndex = methodIndex;
        }

        public LocalReference Location { get; }
        public string UnityEventName { get; }
        public int MethodIndex { get; }


        public bool Equals(ImportedAssetMethodReference other)
        {
            return Location.Equals(other.Location) && UnityEventName == other.UnityEventName && MethodIndex == other.MethodIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is ImportedAssetMethodReference other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Location.GetHashCode();
                hashCode = (hashCode * 397) ^ (UnityEventName != null ? UnityEventName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ MethodIndex;
                return hashCode;
            }
        }

        public void WriteTo(UnsafeWriter writer)
        {
            Location.WriteTo(writer);
            writer.Write(UnityEventName);
            writer.Write(MethodIndex);
        }

        public static ImportedAssetMethodReference ReadFrom(UnsafeReader reader)
        {
            return new ImportedAssetMethodReference(HierarchyReferenceUtil.ReadLocalReferenceFrom(reader), reader.ReadString(), reader.ReadInt());
        }
    }
}