using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Psi;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy
{
    [PolymorphicMarshaller]
    public class AssetDocumentHierarchyElement : IUnityAssetDataElement
    {
        [UsedImplicitly] 
        public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;
        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as AssetDocumentHierarchyElement);


        private static object Read(UnsafeReader reader)
        {
            var count = reader.ReadInt32();
            var result = new AssetDocumentHierarchyElement();

            for (int i = 0; i < count; i++)
            {
                result.AddData(new AssetDocumentHierarchyElement(reader.ReadPolymorphic<IHierarchyElement>()));
            }
            return result;
        }

        private static void Write(UnsafeWriter writer, AssetDocumentHierarchyElement value)
        {
            writer.Write(value.HierarchyElements.Count);
            foreach (var v in value.HierarchyElements)
            {
                writer.WritePolymorphic(v);
            }
        }
        
        public List<IHierarchyElement> HierarchyElements;
        public AssetDocumentHierarchyElement(IHierarchyElement hierarchyElements)
        {
            HierarchyElements = new List<IHierarchyElement>() {hierarchyElements};
        }
        
        public AssetDocumentHierarchyElement()
        {
            HierarchyElements = new List<IHierarchyElement>();
        }
        
        public string ContainerId => nameof(AssetDocumentHierarchyElementContainer);
        
        public void AddData(IUnityAssetDataElement unityAssetDataElement)
        {
            foreach (var element in ((AssetDocumentHierarchyElement)unityAssetDataElement).HierarchyElements)
            {
                HierarchyElements.Add(element);
            }
        }
    }
}