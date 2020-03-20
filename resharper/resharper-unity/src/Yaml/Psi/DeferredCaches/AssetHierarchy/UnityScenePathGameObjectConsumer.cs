using System.Collections.Generic;
using System.Linq;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning;

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
        
        public bool AddGameObject(AssetDocumentHierarchyElement owner, UnityInterningCache cache, IGameObjectHierarchy gameObject)
        {
            myParts.Push(gameObject.GetName(cache) ?? "...");
            myIndex.Push(gameObject.GetTransformHierarchy(cache, owner).NotNull("gameObject.GetTransformHierarchy(cache, owner) != null").GetRootIndex(cache));
            return !myOnlyName;
        }
    }
}