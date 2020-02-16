using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy
{
    [PolymorphicMarshaller]
    public class AssetDocumentHierarchyElement : IUnityAssetDataElement
    {
        private Dictionary<string, IHierarchyElement> myLocalAnchorToHierarchyElement =
            new Dictionary<string, IHierarchyElement>();

        private List<TransformHierarchy> myTransformHierarchies = new List<TransformHierarchy>();

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
                result.myLocalAnchorToHierarchyElement[hierarchyElement.Location.LocalDocumentAnchor] = hierarchyElement;
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
            myLocalAnchorToHierarchyElement[hierarchyElements.Location.LocalDocumentAnchor] = hierarchyElements;
            if (hierarchyElements is TransformHierarchy transformHierarchy)
                myTransformHierarchies.Add(transformHierarchy);
        }
        
        public AssetDocumentHierarchyElement()
        {
        }
        
        public string ContainerId => nameof(AssetDocumentHierarchyElementContainer);
        
        public void AddData(IUnityAssetDataElement unityAssetDataElement)
        {
            var hierarchyElement = (AssetDocumentHierarchyElement)unityAssetDataElement;
            foreach (var element in hierarchyElement.myLocalAnchorToHierarchyElement)
            {
                myLocalAnchorToHierarchyElement[element.Key] = element.Value;
            }
            
            foreach (var element in hierarchyElement.myTransformHierarchies)
            {
                myTransformHierarchies.Add(element);
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

        public void RestoreHierarchy()
        {
            foreach (var transformHierarchy in myTransformHierarchies)
            {
                if (transformHierarchy.GameObjectReference is LocalReference localReference)
                {
                    var go = GetHierarchyElement(localReference.LocalDocumentAnchor) as GameObjectHierarchy;
                    if (go == null)
                        continue;
                    go.Transform = transformHierarchy;
                }
            }
            
            myTransformHierarchies.Clear();
        }
    }
}