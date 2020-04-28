using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Psi;
using JetBrains.Serialization;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetUsages
{
    [PolymorphicMarshaller]
    public class AssetUsagesDataElement : IUnityAssetDataElement
    {
        private readonly List<AssetUsage> myAssetUsages;
        
        [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader)
        {
            var id = reader.ReadLong();
            var count = reader.ReadInt32();
            var result = new AssetUsagesDataElement(id, count);

            for (int i = 0; i < count; i++)
                result.myAssetUsages.Add(AssetUsage.ReadFrom(reader));

            return result;
        }

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as AssetUsagesDataElement);

        private static void Write(UnsafeWriter writer, AssetUsagesDataElement value)
        {
            writer.Write(value.OwnerId);
            writer.Write(value.myAssetUsages.Count);
            foreach (var v in value.myAssetUsages)
            {
                v.WriteTo(writer);
            }
        }

        public AssetUsagesDataElement(IPsiSourceFile sourceFile) : this(sourceFile.PsiStorage.PersistentIndex, 10)
        {
        }

        private AssetUsagesDataElement(long id, int elementsCount)
        {
            OwnerId = id;
            myAssetUsages = new List<AssetUsage>(elementsCount);
        }

        public long OwnerId { get; }
        public string ContainerId => nameof(AssetUsagesElementContainer);
        public void AddData(object data)
        {
            if (data == null)
                return;

            var usages = (LocalList<AssetUsage>) data;
            foreach (var usage in usages)
            {
                myAssetUsages.Add(usage);
            }
        }
        public IEnumerable<AssetUsage> EnumerateAssetUsages()
        {
            foreach (var assetUsage in myAssetUsages)
            {
                yield return assetUsage;
            }
        }
    }
}