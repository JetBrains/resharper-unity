using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy
{
    [PolymorphicMarshaller]
    public class AssetDocumentHierarchyElement : IUnityAssetDataElement
    {
        private readonly Dictionary<ulong, IHierarchyElement> myLocalAnchorToHierarchyElement =
            new Dictionary<ulong, IHierarchyElement>();

        private readonly Dictionary<ulong, ITransformHierarchy> myGameObjectLocationToTransform = new Dictionary<ulong, ITransformHierarchy>(); 
        
        private readonly List<ITransformHierarchy> myTransformHierarchies = new List<ITransformHierarchy>();

        private readonly List<IPrefabInstanceHierarchy>
            myPrefabInstanceHierarchies = new List<IPrefabInstanceHierarchy>();

        [UsedImplicitly] 
        public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;
        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as AssetDocumentHierarchyElement);

        public bool IsScene { get; internal set; }

        public AssetDocumentHierarchyElementContainer AssetDocumentHierarchyElementContainer { get; internal set; }

        private static object Read(UnsafeReader reader)
        {
            var count = reader.ReadInt32();
            var result = new AssetDocumentHierarchyElement();

            for (int i = 0; i < count; i++)
            {
                var hierarchyElement = reader.ReadPolymorphic<IHierarchyElement>();
                result.myLocalAnchorToHierarchyElement[hierarchyElement.Location.LocalDocumentAnchor] = hierarchyElement;
                if (hierarchyElement is ITransformHierarchy transformHierarchy)
                    result.myTransformHierarchies.Add(transformHierarchy);

                if (hierarchyElement is IPrefabInstanceHierarchy prefabInstanceHierarchy)
                    result.myPrefabInstanceHierarchies.Add(prefabInstanceHierarchy);
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
            if (hierarchyElements is ITransformHierarchy transformHierarchy)
                myTransformHierarchies.Add(transformHierarchy);

            if (hierarchyElements is IPrefabInstanceHierarchy prefabInstanceHierarchy)
                myPrefabInstanceHierarchies.Add(prefabInstanceHierarchy);
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
            
            foreach (var element in hierarchyElement.myPrefabInstanceHierarchies)
            {
                myPrefabInstanceHierarchies.Add(element);
            }
        }

        public IHierarchyElement GetHierarchyElement(string ownerGuid, ulong anchor, PrefabImportCache prefabImportCache)
        {
            if (myLocalAnchorToHierarchyElement.TryGetValue(anchor, out var result))
            {
                if (!result.IsStripped || prefabImportCache == null) // stipped means, that element is not real and we should import prefab
                    return result;
            }
            
            if (result != null && IsScene && result.IsStripped )
            {
                var prefabInstance = result.PrefabInstance;
                var correspondingObject = result.CorrespondingSourceObject;
                if (prefabInstance != null && correspondingObject != null)
                    anchor = PrefabsUtil.Import(prefabInstance.LocalDocumentAnchor, correspondingObject.LocalDocumentAnchor);
            }

            if (prefabImportCache != null)
            {
                var elements = prefabImportCache.GetImportedElementsFor(ownerGuid, this);
                
                if (elements.TryGetValue(anchor, out var importedResult))
                    return importedResult;
            }
            
            return null;
        }

        public List<IPrefabInstanceHierarchy> PrefabInstanceHierarchies => myPrefabInstanceHierarchies;

        public void RestoreHierarchy()
        {
            foreach (var transformHierarchy in myTransformHierarchies)
            {
                var reference = transformHierarchy.GameObjectReference;
                if (reference != null)
                {
                    myGameObjectLocationToTransform[reference.LocalDocumentAnchor] = transformHierarchy;
                }
            }
            
            myTransformHierarchies.Clear();
        }

        internal ITransformHierarchy GetTransformHierarchy(GameObjectHierarchy gameObjectHierarchy)
        {
            return myGameObjectLocationToTransform.GetValueSafe(gameObjectHierarchy.Location.LocalDocumentAnchor);
        }

        public IEnumerable<IHierarchyElement> Elements => myLocalAnchorToHierarchyElement.Values;
    }
}