using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Stripped;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy
{
    public partial class AssetDocumentHierarchyElement : IUnityAssetDataElement
    {
        
        private readonly List<IHierarchyElement> myOtherElements = new List<IHierarchyElement>();
        
        // avoid boxing
        private readonly List<TransformHierarchy> myTransformElements = new List<TransformHierarchy>();
        private readonly List<ScriptComponentHierarchy> myScriptComponentElements = new List<ScriptComponentHierarchy>();
        private readonly List<ComponentHierarchy> myComponentElements = new List<ComponentHierarchy>();
        private readonly List<GameObjectHierarchy> myGameObjectHierarchies = new List<GameObjectHierarchy>();

        private readonly Dictionary<ulong, int> myGameObjectLocationToTransform = new Dictionary<ulong, int>(); 
        

        private readonly List<int> myPrefabInstanceHierarchies = new List<int>();


        public bool IsScene { get; internal set; }

        public AssetDocumentHierarchyElementContainer AssetDocumentHierarchyElementContainer { get; internal set; }
        
        public AssetDocumentHierarchyElement()
        {
        }

        public void AddHierarchyElement(IHierarchyElement hierarchyElement)
        {
            myOtherElements.Add(hierarchyElement);
        }

        // do not use interface here! avoid boxing
        public void AddTransformElement(TransformHierarchy hierarchyElement)
        {
            myTransformElements.Add(hierarchyElement);
        }
       
        public void AddGameObjectElement(GameObjectHierarchy hierarchyElement)
        {
            myGameObjectHierarchies.Add(hierarchyElement);
        }
        
        public void AddScriptComponentElement(ScriptComponentHierarchy hierarchyElement)
        {
            myScriptComponentElements.Add(hierarchyElement);
        }
        
        public void AddComponentElement(ComponentHierarchy hierarchyElement)
        {
            myComponentElements.Add(hierarchyElement);
        }
        
        
        
        public string ContainerId => nameof(AssetDocumentHierarchyElementContainer);
        
        public void AddData(IUnityAssetDataElement unityAssetDataElement)
        {
            var hierarchyElement = (AssetDocumentHierarchyElement)unityAssetDataElement;

            foreach (var element in hierarchyElement.myOtherElements)
            {
                myOtherElements.Add(element);
            }
            
            foreach (var element in hierarchyElement.myTransformElements)
            {
                myTransformElements.Add(element);
            }
            
            foreach (var element in hierarchyElement.myScriptComponentElements)
            {
                myScriptComponentElements.Add(element);
            }
            
            foreach (var element in hierarchyElement.myComponentElements)
            {
                myComponentElements.Add(element);
            }
            
            foreach (var element in hierarchyElement.myGameObjectHierarchies)
            {
                myGameObjectHierarchies.Add(element);
            }
        }

        public IHierarchyElement GetHierarchyElement(string ownerGuid, ulong anchor, UnityInterningCache unityInterningCache, PrefabImportCache prefabImportCache)
        {
            var result = SearchForAnchor(unityInterningCache, anchor);
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

        // boxing is not problem here
        private IHierarchyElement SearchForAnchor(UnityInterningCache unityInterningCache, ulong anchor)
        {
            return
                SearchForAnchor(myGameObjectHierarchies, unityInterningCache, anchor) ??
                SearchForAnchor(myTransformElements, unityInterningCache, anchor) ??
                SearchForAnchor(myScriptComponentElements, unityInterningCache, anchor) ??
                SearchForAnchor(myComponentElements, unityInterningCache, anchor) ??
                SearchForAnchor(myOtherElements, unityInterningCache, anchor);
        }


        private IHierarchyElement SearchForAnchor<T>(List<T> elements, UnityInterningCache cache, ulong anchor) where T : IHierarchyElement
        {
            var searchResult = elements.BinarySearchEx(a => a.GetLocation(cache).LocalDocumentAnchor.CompareTo(anchor));
            if (searchResult.IsHit)
                return searchResult.HitItem;

            return null;
        }
        

        public IEnumerable<IPrefabInstanceHierarchy> GetPrefabInstanceHierarchies()
        {
            for (int i = 0; i < myPrefabInstanceHierarchies.Count; i++)
            {
                var element = GetElementByInternalIndex(i);
                if (element != null)
                    Assertion.Assert(element is IPrefabInstanceHierarchy, "element is IPrefabInstanceHierarchy");
                yield return element as IPrefabInstanceHierarchy;
            }
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
 
                var offset = 0;
                // concating arrays to one by index. see GetElementByInternalIndex too
                FillIndices(myOtherElements, offset, unityInterningCache);
                offset += myOtherElements.Count;
                
                FillIndices(myTransformElements, offset, unityInterningCache);
                offset += myTransformElements.Count;

                FillIndices(myGameObjectHierarchies, offset, unityInterningCache);
                offset += myGameObjectHierarchies.Count;

                FillIndices(myComponentElements, offset, unityInterningCache);
                offset += myComponentElements.Count;
                
                FillIndices(myScriptComponentElements, offset, unityInterningCache);
                offset += myScriptComponentElements.Count;

            }
        }


        private void FillIndices<T>(List<T> list, int curOffset, UnityInterningCache unityInterningCache) where  T : IHierarchyElement
        {
            for (int i = 0; i < list.Count; i++)
            {
                var element = list[i];
                if (element is ITransformHierarchy transformHierarchy)
                {
                    var reference = transformHierarchy.GetOwner(unityInterningCache);
                    if (reference != null)
                    {
                        myGameObjectLocationToTransform[reference.LocalDocumentAnchor] = curOffset + i;
                    }
                }

                if (element is IPrefabInstanceHierarchy prefabInstanceHierarchy)
                    myPrefabInstanceHierarchies.Add(curOffset + i);
            }
            
            list.Sort((a, b) => a.GetLocation(unityInterningCache).LocalDocumentAnchor.
                CompareTo(b.GetLocation(unityInterningCache).LocalDocumentAnchor));
        }

        internal ITransformHierarchy GetTransformHierarchy(UnityInterningCache cache, GameObjectHierarchy gameObjectHierarchy)
        {
            var transformIndex = myGameObjectLocationToTransform.GetValueSafe(gameObjectHierarchy.GetLocation(cache).LocalDocumentAnchor, -1);
            if (transformIndex == -1)
                return null;

            var element = GetElementByInternalIndex(transformIndex);
            if (element != null)
                Assertion.Assert(element is ITransformHierarchy, "element is ITransformHierarchy");
            
            return element as ITransformHierarchy;
        }

        private IHierarchyElement GetElementByInternalIndex(int index)
        {
            if (index < myOtherElements.Count)
                return myOtherElements[index];

            index -= myOtherElements.Count;
            
            if (index < myTransformElements.Count)
                return myTransformElements[index];

            index -= myTransformElements.Count;
            
            if (index < myGameObjectHierarchies.Count)
                return myGameObjectHierarchies[index];

            index -= myGameObjectHierarchies.Count;
            
            if (index < myComponentElements.Count)
                return myComponentElements[index];

            index -= myComponentElements.Count;
            
            if (index < myScriptComponentElements.Count)
                return myScriptComponentElements[index];

            index -= myScriptComponentElements.Count;
            
            
            throw new IndexOutOfRangeException("Index was out of range in concated array");
        }

        public IEnumerable<IHierarchyElement> Elements()
        {
            foreach (var otherElement in myOtherElements)
                yield return otherElement;
            
            foreach (var otherElement in myTransformElements)
                yield return otherElement;
            
            foreach (var otherElement in myGameObjectHierarchies)
                yield return otherElement;
            
            foreach (var otherElement in myComponentElements)
                yield return otherElement;
            
            foreach (var otherElement in myScriptComponentElements)
                yield return otherElement;
        }
    }
}