using System.Text;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util.dataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    public static class UnityObjectPsiUtil
    {
        [NotNull]
        public static string GetComponentName([NotNull] IYamlDocument componentDocument)
        {
            // If name is null, and fileid is external, just use component type ("MonoBehaviour")
            var name = componentDocument.GetUnityObjectPropertyValue("m_Name").AsString();
            if (string.IsNullOrWhiteSpace(name))
            {
                var scriptDocument = componentDocument.GetUnityObjectDocumentFromFileIDProperty("m_Script");
                if (scriptDocument == null)
                    return componentDocument.GetUnityObjectTypeFromRootNode() ?? "Component";

                return scriptDocument.GetUnityObjectPropertyValue("m_Name").AsString()
                       ?? scriptDocument.GetUnityObjectTypeFromRootNode()
                       ?? "Component";
            }
            return name;
        }

        [NotNull]
        public static string GetGameObjectPath([NotNull] IYamlDocument componentDocument)
        {
            // component.m_GameObject gives fileID
            // gameObject.m_Component.component[i] is fileID to RectTransform
            // transform.m_Father gives fileID to parent RectTransform
            // parent.m_GameObject gives fileID to parent
            // gameObject.m_Name is name
            var parts = new FrugalLocalList<string>();

            var gameObjectDocument = componentDocument.GetUnityObjectDocumentFromFileIDProperty("m_GameObject");
            do
            {
                var name = gameObjectDocument.GetUnityObjectPropertyValue("m_Name").AsString() ?? "GameObject";
                parts.Add(name);
                gameObjectDocument = GetParentGameObject(gameObjectDocument);
            } while (gameObjectDocument != null);

            // This will only happen if the given component doesn't have a game object, which is weird
            if (parts.Count == 0)
                return "GameObject";

            if (parts.Count == 1)
                return parts[0];

            var sb = new StringBuilder();
            for (var i = parts.Count - 1; i >= 0; i--)
            {
                sb.Append(parts[i]);
                sb.Append("\\");
            }

            return sb.ToString();
        }

        [CanBeNull]
        private static IYamlDocument FindTransformComponentForGameObject([CanBeNull] IYamlDocument gameObjectDocument)
        {
            // GameObject:
            //   m_Component:
            //   - component: {fileID: 1234567890}
            //   - component: {fileID: 1234567890}
            //   - component: {fileID: 1234567890}
            // One of these components is the RectTransform. Most likely the first, but we can't rely on order
            if (gameObjectDocument?.GetUnityObjectPropertyValue("m_Component") is IBlockSequenceNode components)
            {
                var file = (IYamlFile) gameObjectDocument.GetContainingFile();

                foreach (var componentEntry in components.EntriesEnumerable)
                {
                    // - component: {fileID: 1234567890}
                    var componentNode = componentEntry.Value as IBlockMappingNode;
                    var componentFileID = componentNode?.EntriesEnumerable.FirstOrDefault()?.Value.AsFileID();
                    if (componentFileID != null && !componentFileID.IsNullReference && !componentFileID.IsExternal)
                    {
                        var component = file.FindDocumentByAnchor(componentFileID.fileID);
                        if (component.GetUnityObjectTypeFromRootNode() == "RectTransform")
                            return component;
                    }
                }
            }

            return null;
        }

        [CanBeNull]
        private static IYamlDocument GetParentGameObject([CanBeNull] IYamlDocument gameObjectDocument)
        {
            var transformDocument = FindTransformComponentForGameObject(gameObjectDocument);
            var parentTransformDocument = transformDocument.GetUnityObjectDocumentFromFileIDProperty("m_Father");
            return parentTransformDocument.GetUnityObjectDocumentFromFileIDProperty("m_GameObject");
        }
    }
}