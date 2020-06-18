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
        private readonly List<AssetScriptUsages> myAssetUsages;
        
        [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader)
        {
            var count = reader.ReadInt32();
            var result = new AssetUsagesDataElement(count);

            for (int i = 0; i < count; i++)
                result.myAssetUsages.Add(AssetScriptUsages.ReadFrom(reader));

            return result;
        }

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as AssetUsagesDataElement);

        private static void Write(UnsafeWriter writer, AssetUsagesDataElement value)
        {
            writer.Write(value.myAssetUsages.Count);
            foreach (var v in value.myAssetUsages)
            {
                v.WriteTo(writer);
            }
        }

        public AssetUsagesDataElement() : this(10)
        {
        }

        private AssetUsagesDataElement(int elementsCount)
        {
            myAssetUsages = new List<AssetScriptUsages>(elementsCount);
        }

        public string ContainerId => nameof(AssetScriptUsagesElementContainer);
        public void AddData(object data)
        {
            if (data == null)
                return;

            var usages = (LocalList<AssetScriptUsages>) data;
            foreach (var usage in usages)
            {
                myAssetUsages.Add(usage);
            }
        }
        public IEnumerable<AssetScriptUsages> EnumerateAssetUsages()
        {
            foreach (var assetUsage in myAssetUsages)
            {
                yield return assetUsage;
            }
        }
    }
}