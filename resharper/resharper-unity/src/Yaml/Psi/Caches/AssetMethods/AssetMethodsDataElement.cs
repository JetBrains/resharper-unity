using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Features.Inspections.Resources;
using JetBrains.ReSharper.Psi;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetMethods
{
    [PolymorphicMarshaller]
    public class AssetMethodsDataElement : IUnityAssetDataElement
    {
        public readonly List<AssetMethodData> Methods;
        [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader)
        {
            var count = reader.ReadInt32();
            var methods = new List<AssetMethodData>();
            for (int i = 0; i < count; i++)
                methods.Add(AssetMethodData.ReadFrom(reader));
            
            return new AssetMethodsDataElement(methods);
        }

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as AssetMethodsDataElement);

        private static void Write(UnsafeWriter writer, AssetMethodsDataElement value)
        {
            writer.Write(value.Methods.Count);
            foreach (var v in value.Methods)
            {
                v.WriteTo(writer);
            }
        }

        public AssetMethodsDataElement(List<AssetMethodData> methods)
        {
            Methods = methods;
        }

        public string ContainerId => nameof(AssetMethodsElementContainer);
        public void AddData(IUnityAssetDataElement unityAssetDataElement)
        {
            foreach (var assetMethodData in ((AssetMethodsDataElement) unityAssetDataElement).Methods)
            {
                Methods.Add(assetMethodData);
            }
        }
    }
}