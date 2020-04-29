using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Collections;
using JetBrains.ReSharper.Psi;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents
{
    [PolymorphicMarshaller]
    public class UnityEventsDataElement : IUnityAssetDataElement
    {
        public readonly List<UnityEventData> UnityEvents;
        public readonly ImportedUnityEventData ImportedUnityEventData;
        
        [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader)
        {
            var id = reader.ReadLong();
            var count = reader.ReadInt32();
            var methods = new List<UnityEventData>(count);
            for (int i = 0; i < count; i++)
                methods.Add(UnityEventData.ReadFrom(reader));
            
            var result =  new UnityEventsDataElement(id, methods, ImportedUnityEventData.ReadFrom(reader));
            return result;
        }

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as UnityEventsDataElement);

        private static void Write(UnsafeWriter writer, UnityEventsDataElement value)
        {
            writer.Write(value.OwnerId);
            writer.Write(value.UnityEvents.Count);
            foreach (var v in value.UnityEvents)
            {
                v.WriteTo(writer);
            }

            value.ImportedUnityEventData.WriteTo(writer);

        }

        public UnityEventsDataElement(IPsiSourceFile sourceFile) : this(sourceFile.PsiStorage.PersistentIndex, new List<UnityEventData>(), new ImportedUnityEventData())
        {
        }

        private UnityEventsDataElement(long index, List<UnityEventData> unityEventData,
            ImportedUnityEventData importedUnityEventData)
        {
            OwnerId = index;
            UnityEvents = unityEventData;
            ImportedUnityEventData = importedUnityEventData;
        }
        
        public long OwnerId { get; }
        public string ContainerId => nameof(UnityEventsElementContainer);
        
        public void AddData(object result)
        {
            if (result == null)
                return;

            var buildResult = (UnityEventsBuildResult) result;
            var methods = buildResult.UnityEventData;
            foreach (var assetMethodData in methods)
            {
                UnityEvents.Add(assetMethodData);
            }

            foreach (var (reference, values) in buildResult.ModificationDescription.ReferenceToImportedData)
            {
                ImportedUnityEventData.ReferenceToImportedData[reference] = values;
            }
        }
    }
}