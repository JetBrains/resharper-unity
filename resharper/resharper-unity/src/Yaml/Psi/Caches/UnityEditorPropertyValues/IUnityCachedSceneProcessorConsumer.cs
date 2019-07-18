using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Swa
{
    public interface IUnityCachedSceneProcessorConsumer
    {
        void ConsumeGameObject(GameObjectHierarchyElement gameObject, TransformHierarchyElement transformHierarchyElement, ModificationHierarchyElement modifications);
    }
    
    public class UnityPathCachedSceneConsumer : IUnityCachedSceneProcessorConsumer
    {
        public List<string> NameParts => myParts.ToList();

        private readonly Stack<string> myParts = new Stack<string>();
        
        public void ConsumeGameObject(GameObjectHierarchyElement gameObject, TransformHierarchyElement transformHierarchyElement, ModificationHierarchyElement modifications)
        {
            string name = null;
            if (modifications != null)
            {
                name = modifications.GetName(new FileID(null, gameObject.Id.fileID));
            }
            if (name == null)
            {
                name = gameObject.Name;
            }

            if (name?.Equals(string.Empty) == true)
                name = null;
            
            myParts.Push(name ?? "...");
        }
    }
}