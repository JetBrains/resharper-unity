using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Collections;
using JetBrains.ReSharper.Psi;
using JetBrains.Serialization;
using JetBrains.Util;

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
            var count = reader.ReadInt32();
            var methods = new List<UnityEventData>(count);
            for (int i = 0; i < count; i++)
                methods.Add(UnityEventData.ReadFrom(reader));
            
            var result =  new UnityEventsDataElement(methods, ImportedUnityEventData.ReadFrom(reader));
            return result;
        }

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as UnityEventsDataElement);

        private static void Write(UnsafeWriter writer, UnityEventsDataElement value)
        {
            writer.Write(value.UnityEvents.Count);
            foreach (var v in value.UnityEvents)
            {
                v.WriteTo(writer);
            }

            value.ImportedUnityEventData.WriteTo(writer);

        }

        public UnityEventsDataElement() : this(new List<UnityEventData>(), new ImportedUnityEventData())
        {
        }

        private UnityEventsDataElement(List<UnityEventData> unityEventData,
            ImportedUnityEventData importedUnityEventData)
        {
            UnityEvents = unityEventData;
            ImportedUnityEventData = importedUnityEventData;
        }
        
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

            ImportedUnityEventData.HasEventModificationWithoutMethodName |= buildResult.ModificationDescription.HasEventModificationWithoutMethodName;
            ImportedUnityEventData.AssetMethodNameInModifications.AddRange(buildResult.ModificationDescription.AssetMethodNameInModifications);
            foreach (var (key, values) in buildResult.ModificationDescription.UnityEventToModifiedIndex)
            {
                ImportedUnityEventData.UnityEventToModifiedIndex.AddRange(key, values);
            }
        }
    }
}