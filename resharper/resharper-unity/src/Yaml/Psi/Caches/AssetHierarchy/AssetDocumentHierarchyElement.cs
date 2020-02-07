using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.Elements;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy
{
    [PolymorphicMarshaller]
    public class AssetDocumentHierarchyElement : IUnityAssetDataElement
    {
        private Dictionary<string, IHierarchyElement> myLocalAnchorToHierarchyElement =
            new Dictionary<string, IHierarchyElement>();

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
                var hierarchyElement = reader.ReadPolymorphic<IHierarchyElement>();
                result.myLocalAnchorToHierarchyElement[hierarchyElement.LocalReference.LocalDocumentAnchor] = hierarchyElement;
            }
            return result;
        }

        private static void Write(UnsafeWriter writer, AssetDocumentHierarchyElement value)
        {
            writer.Write(value.myLocalAnchorToHierarchyElement.Count);
            foreach (var v in value.myLocalAnchorToHierarchyElement)
            {
                writer.WritePolymorphic(v.Value);
            }
        }
        public AssetDocumentHierarchyElement(IHierarchyElement hierarchyElements)
        {
            myLocalAnchorToHierarchyElement[hierarchyElements.LocalReference.LocalDocumentAnchor] = hierarchyElements;
        }
        
        public AssetDocumentHierarchyElement()
        {
        }
        
        public string ContainerId => nameof(AssetDocumentHierarchyElementContainer);
        
        public void AddData(IUnityAssetDataElement unityAssetDataElement)
        {
            foreach (var element in ((AssetDocumentHierarchyElement)unityAssetDataElement).myLocalAnchorToHierarchyElement)
            {
                myLocalAnchorToHierarchyElement[element.Key] = element.Value;
            }
        }

        public IHierarchyElement GetHierarchyElement(string anchor)
        {
            if (myLocalAnchorToHierarchyElement.TryGetValue(anchor, out var result))
            {
                return result;
            }

            return null;
        }
    }
}