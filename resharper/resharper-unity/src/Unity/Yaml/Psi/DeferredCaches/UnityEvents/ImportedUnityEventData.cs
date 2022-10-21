using System.Collections.Generic;
using JetBrains.Collections;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents
{
    public class ImportedUnityEventData
    {
        public readonly OneToSetMap<(LocalReference Location, string EventName), int> UnityEventToModifiedIndex = new();
        public readonly HashSet<string> AssetMethodNameInModifications = new();
        public readonly Dictionary<LocalReference, AssetMethodUsages> AssetMethodUsagesSet = new();
        public bool HasEventModificationWithoutMethodName { get; set; }

        public void WriteTo(UnsafeWriter writer)
        {
            writer.Write(HasEventModificationWithoutMethodName);
            writer.Write(UnityEventToModifiedIndex.Count);
            foreach (var (key, values) in UnityEventToModifiedIndex)
            {
                key.Location.WriteTo(writer);
                writer.Write(key.EventName);
                writer.Write(values.Count);
                foreach (var v in values)
                {
                    writer.Write(v);
                }
            }
            
            writer.Write(AssetMethodNameInModifications.Count);
            foreach (var name in AssetMethodNameInModifications)
                writer.Write(name);
            
            writer.Write(AssetMethodUsagesSet.Count);
            foreach (var item in AssetMethodUsagesSet)
            {
                item.Key.WriteTo(writer);
                item.Value.WriteTo(writer);
            }
        }

        public static ImportedUnityEventData ReadFrom(UnsafeReader unsafeReader)
        {
            var result = new ImportedUnityEventData();
            result.HasEventModificationWithoutMethodName = unsafeReader.ReadBool();
            var count = unsafeReader.ReadInt();
            for (int i = 0; i < count; i++)
            {
                var key = (HierarchyReferenceUtil.ReadLocalReferenceFrom(unsafeReader), unsafeReader.ReadString());
                var setCount = unsafeReader.ReadInt();
                for (int j = 0; j < setCount; j++)
                {
                    result.UnityEventToModifiedIndex.Add(key, unsafeReader.ReadInt());
                }
            }

            var methodsCount = unsafeReader.ReadInt();
            for (int i = 0; i < methodsCount; i++)
                result.AssetMethodNameInModifications.Add(unsafeReader.ReadString());

            var assetUsagesCount = unsafeReader.ReadInt();
            for (int i = 0; i < assetUsagesCount; i++)
            {
                var key = HierarchyReferenceUtil.ReadLocalReferenceFrom(unsafeReader);
                var value = AssetMethodUsages.ReadFrom(unsafeReader);
                result.AssetMethodUsagesSet.Add(key, value);
            }
            
            return result;
        }
    }
}