using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Stripped;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy
{
    public partial class AssetDocumentHierarchyElement : IUnityAssetDataElement
    {
        
        private readonly List<IHierarchyElement> myElements = new List<IHierarchyElement>();
        
        private readonly Dictionary<ulong, int> myGameObjectLocationToTransform = new Dictionary<ulong, int>(); 
        

        private readonly List<int> myPrefabInstanceHierarchies = new List<int>();


        public bool IsScene { get; internal set; }

        public AssetDocumentHierarchyElementContainer AssetDocumentHierarchyElementContainer { get; internal set; }
        
        public AssetDocumentHierarchyElement(IHierarchyElement hierarchyElements)
        {
            myElements.Add(hierarchyElements);
        }
        
        public AssetDocumentHierarchyElement()
        {
        }
        
        public string ContainerId => nameof(AssetDocumentHierarchyElementContainer);
        
        public void AddData(IUnityAssetDataElement unityAssetDataElement)
        {
            var hierarchyElement = (AssetDocumentHierarchyElement)unityAssetDataElement;
            foreach (var element in hierarchyElement.myElements)
            {
                myElements.Add(element);
            }
        }

        public IHierarchyElement GetHierarchyElement(string ownerGuid, ulong anchor, UnityInterningCache unityInterningCache, PrefabImportCache prefabImportCache)
        {
            var searchResult = myElements.BinarySearchEx(a => a.GetLocation(unityInterningCache).LocalDocumentAnchor.CompareTo(anchor));
            var result = searchResult.IsHit ? searchResult.HitItem : null;
            if (result != null)
            {
                if (!(result is IStrippedHierarchyElement) || prefabImportCache == null) // stipped means, that element is not real and we should import prefab
                    return result;
            }
            
            if (result != null && IsScene && result is IStrippedHierarchyElement strippedHierarchyElement )
            {
                var prefabInstance = strippedHierarchyElement.GetPrefabInstance(unityInterningCache);
                var correspondingObject = strippedHierarchyElement.GetCoresspondingSourceObject(unityInterningCache);
                if (prefabInstance != null && correspondingObject != null)
                    anchor = PrefabsUtil.Import(prefabInstance.LocalDocumentAnchor, correspondingObject.LocalDocumentAnchor);
            }

            if (prefabImportCache != null)
            {
                var elements = prefabImportCache.GetImportedElementsFor(unityInterningCache, ownerGuid, this);
                
                if (elements.TryGetValue(anchor, out var importedResult))
                    return importedResult;
            }
            
            return null;
        }

        public IEnumerable<IPrefabInstanceHierarchy> GetPrefabInstanceHierarchies()
        {
            for (int i = 0; i < myPrefabInstanceHierarchies.Count; i++)
                yield return myElements[i] as IPrefabInstanceHierarchy;
        }

        private readonly object myLockObject = new object();
        private volatile bool myIsRestored = false;
        public void RestoreHierarchy(UnityInterningCache unityInterningCache)
        {
            if (myIsRestored)
                return;
            
            lock (myLockObject)
            {
                if (myIsRestored)
                    return;
                
                myIsRestored = true;
                for (int i = 0; i < myElements.Count; i++)
                {
                    var element = myElements[i];
                    if (element is ITransformHierarchy transformHierarchy)
                    {
                        var reference = transformHierarchy.GetOwner(unityInterningCache);
                        if (reference != null)
                        {
                            myGameObjectLocationToTransform[reference.LocalDocumentAnchor] = i;
                        }
                    }

                    if (element is IPrefabInstanceHierarchy prefabInstanceHierarchy)
                        myPrefabInstanceHierarchies.Add(i);
                }
                
                myElements.Sort((a, b) => a.GetLocation(unityInterningCache).LocalDocumentAnchor.
                    CompareTo(b.GetLocation(unityInterningCache).LocalDocumentAnchor));

            }
        }

        internal ITransformHierarchy GetTransformHierarchy(UnityInterningCache cache, GameObjectHierarchy gameObjectHierarchy)
        {
            var transformIndex = myGameObjectLocationToTransform.GetValueSafe(gameObjectHierarchy.GetLocation(cache).LocalDocumentAnchor, -1);
            if (transformIndex == -1)
                return null;
            return myElements[transformIndex] as ITransformHierarchy;
        }

        public IEnumerable<IHierarchyElement> Elements => myElements;
    }
}