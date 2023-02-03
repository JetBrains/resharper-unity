using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Serialization;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetScriptUsages
{
    [PolymorphicMarshaller]
    public class AssetScriptUsagesDataElement : IUnityAssetDataElement
    {
        private readonly List<AssetScriptUsage> myAssetUsages;
        
        [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader)
        {
            var count = reader.ReadInt32();
            var result = new AssetScriptUsagesDataElement(count);

            for (int i = 0; i < count; i++)
                result.myAssetUsages.Add(AssetScriptUsage.ReadFrom(reader));

            return result;
        }

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as AssetScriptUsagesDataElement);

        private static void Write(UnsafeWriter writer, AssetScriptUsagesDataElement value)
        {
            writer.Write(value.myAssetUsages.Count);
            foreach (var v in value.myAssetUsages)
            {
                v.WriteTo(writer);
            }
        }

        public AssetScriptUsagesDataElement() : this(10)
        {
        }

        private AssetScriptUsagesDataElement(int elementsCount)
        {
            myAssetUsages = new List<AssetScriptUsage>(elementsCount);
        }

        public string ContainerId => nameof(AssetScriptUsagesElementContainer);
        public void AddData(object data)
        {
            if (data == null)
                return;

            var usages = (LocalList<AssetScriptUsage>) data;
            foreach (var usage in usages)
            {
                myAssetUsages.Add(usage);
            }
        }
        
        [NotNull]
        public IEnumerable<AssetScriptUsage> EnumerateAssetUsages()
        {
            return myAssetUsages;
        }
    }
}