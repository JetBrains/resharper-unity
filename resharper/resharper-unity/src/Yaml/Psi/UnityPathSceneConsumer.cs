using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    public class UnityPathSceneConsumer : IUnitySceneProcessorConsumer
    {
        public readonly List<string> NameParts = new List<string>();
        public void ConsumeGameObject(IYamlDocument gameObject, IBlockMappingNode modifications)
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
            NameParts.Add(name ?? "Unknown");
        }
    }
}