using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetScriptUsages;
using JetBrains.Serialization;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimatorUsages
{
    [PolymorphicMarshaller]
    public class AnimatorGameObjectDataElement : IUnityAssetDataElement
    {
        // copy-paste of the AssetScriptUsagesDataElement, with a different ContainerId
        
        public string ContainerId => nameof(AnimatorGameObjectUsagesContainer);
        private readonly List<AssetScriptUsage> myAssetUsages;
        
        [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader)
        {
            var count = reader.ReadInt32();
            var result = new AnimatorGameObjectDataElement(count);

            for (int i = 0; i < count; i++)
                result.myAssetUsages.Add(AssetScriptUsage.ReadFrom(reader));

            return result;
        }

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as AnimatorGameObjectDataElement);

        private static void Write(UnsafeWriter writer, AnimatorGameObjectDataElement value)
        {
            writer.Write(value.myAssetUsages.Count);
            foreach (var v in value.myAssetUsages)
            {
                v.WriteTo(writer);
            }
        }

        public AnimatorGameObjectDataElement() : this(10)
        {
        }

        private AnimatorGameObjectDataElement(int elementsCount)
        {
            myAssetUsages = new List<AssetScriptUsage>(elementsCount);
        }
        
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