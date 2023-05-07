using System.Collections.Generic;
using System.Linq;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy
{
    public class UnityScenePathGameObjectConsumer : IGameObjectConsumer
    {
        private readonly bool myOnlyName;

        public UnityScenePathGameObjectConsumer(bool onlyName = false)
        {
            myOnlyName = onlyName;
        }
    
        public List<string> NameParts => myParts.ToList();
        public List<int> RootIndexes => myIndex.ToList();
        
        private readonly Stack<string> myParts = new Stack<string>();
        private readonly Stack<int> myIndex = new Stack<int>();
        
        public bool AddGameObject(AssetDocumentHierarchyElement owner, IGameObjectHierarchy gameObject, AssetDocumentHierarchyElementContainer container, bool importPrefab)
        {
            myParts.Push(gameObject.Name ?? "...");
            var transformHierarchy = gameObject.GetTransformHierarchy(owner).NotNull("gameObject.GetTransformHierarchy(cache, owner) != null");
            if (transformHierarchy.RootOrder != -1)
            {
                myIndex.Push(transformHierarchy.RootOrder);
            }
            else
            {
                // index of the transform may instead come from the m_Children of its parent
                var currentTransform = gameObject.GetTransformHierarchy(owner);
                var parentTransformReference = currentTransform.ParentTransform;
                var parentTransform = container.GetHierarchyElement(parentTransformReference, importPrefab);
                if (parentTransform is TransformHierarchy parentTransformHierarchy)
                {
                    myIndex.Push(parentTransformHierarchy.Children.IndexOf(currentTransform.Location.LocalDocumentAnchor));
                }
            }
            return !myOnlyName;
        }
    }
}