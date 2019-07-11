using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Swa;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    public class UnityPathSceneConsumer : IUnitySceneProcessorConsumer
    {
        private readonly bool myOnlyName;

        public UnityPathSceneConsumer(bool onlyName = false)
        {
            myOnlyName = onlyName;
        }

        public List<string> NameParts => myParts.ToList();
        
        private Stack<string> myParts = new Stack<string>();
        
        public bool ConsumeGameObject(IYamlDocument gameObject, IBlockMappingNode modifications)
        {
            string name = null;
            if (modifications != null)
            {
                var documentId = gameObject.GetFileId();
                name = UnityObjectPsiUtil.GetValueFromModifications(modifications, documentId, UnityYamlConstants.NameProperty);
            }
            if (name == null)
            {
                name = gameObject.GetUnityObjectPropertyValue(UnityYamlConstants.NameProperty).AsString();
            }

            if (name?.Equals(string.Empty) == true)
                name = null;
            
            myParts.Push(name ?? "Unknown");


            return !myOnlyName;
        }
    }
}