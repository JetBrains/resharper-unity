using System.Collections.Generic;
using System.Linq;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues
{
    public interface IUnityCachedSceneProcessorConsumer
    {
        void ConsumeGameObject(GameObjectHierarchyElement gameObject, TransformHierarchyElement transformHierarchyElement, ModificationHierarchyElement modifications);
    }
    
    public class UnityPathCachedSceneConsumer : IUnityCachedSceneProcessorConsumer
    {
        public List<string> NameParts => myParts.ToList();
        public List<int> RootIndexes => myIndex.ToList();

        private readonly Stack<string> myParts = new Stack<string>();
        private readonly Stack<int> myIndex = new Stack<int>();
        
        public void ConsumeGameObject(GameObjectHierarchyElement gameObject, TransformHierarchyElement transformHierarchyElement, ModificationHierarchyElement modifications)
        {
            string name = null;
            if (modifications != null)
            {
                name = modifications.GetName(new AssetDocumentReference(null, gameObject.Id.LocalDocumentAnchor));
            }
            if (name == null)
            {
                name = gameObject.Name;
            }

            if (name?.Equals(string.Empty) == true)
                name = null;
            
            myParts.Push(name ?? "...");
            
            int? rootOrder = null;
            if (modifications != null)
            {
                rootOrder = modifications.GetRootIndex(transformHierarchyElement.Id.WithGuid(null));
            }
            
            if (rootOrder == null)
            {
                rootOrder = transformHierarchyElement.RootOrder;
            }
            
            myIndex.Push(rootOrder ?? 0);

        }
    }
}