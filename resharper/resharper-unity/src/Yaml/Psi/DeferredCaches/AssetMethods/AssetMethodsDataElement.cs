using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Psi;
using JetBrains.Serialization;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetMethods
{
    [PolymorphicMarshaller]
    public class AssetMethodsDataElement : IUnityAssetDataElement
    {
        public readonly List<AssetMethodData> Methods = new List<AssetMethodData>();
        [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader)
        {
            var id = reader.ReadLong();
            var count = reader.ReadInt32();
            var methods = new LocalList<AssetMethodData>();
            for (int i = 0; i < count; i++)
                methods.Add(AssetMethodData.ReadFrom(reader));
            
            var result =  new AssetMethodsDataElement(id);
            result.AddData(methods);
            return result;
        }

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as AssetMethodsDataElement);

        private static void Write(UnsafeWriter writer, AssetMethodsDataElement value)
        {
            writer.Write(value.OwnerId);
            writer.Write(value.Methods.Count);
            foreach (var v in value.Methods)
            {
                v.WriteTo(writer);
            }
        }

        public AssetMethodsDataElement(IPsiSourceFile sourceFile) : this(sourceFile.PsiStorage.PersistentIndex)
        {
        }

        private AssetMethodsDataElement(long index)
        {
            OwnerId = index;
        }
        
        public long OwnerId { get; }
        public string ContainerId => nameof(AssetMethodsElementContainer);
        
        public void AddData(object methodsData)
        {
            if (methodsData == null)
                return;
            
            var methods = (LocalList<AssetMethodData>) methodsData;
            foreach (var assetMethodData in methods)
            {
                Methods.Add(assetMethodData);
            }
        }
    }
}