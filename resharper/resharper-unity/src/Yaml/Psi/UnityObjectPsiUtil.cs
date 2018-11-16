using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.dataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    public static class UnityObjectPsiUtil
    {
        [NotNull]
        public static string GetComponentName([NotNull] IYamlDocument componentDocument)
        {
            var name = componentDocument.GetUnityObjectPropertyValue("m_Name").AsString();
            if (!string.IsNullOrWhiteSpace(name))
                return name;

            var scriptDocument = componentDocument.GetUnityObjectDocumentFromFileIDProperty("m_Script");
            name = scriptDocument.GetUnityObjectPropertyValue("m_Name").AsString();
            if (!string.IsNullOrWhiteSpace(name))
                return name;

            var fileID = componentDocument.GetUnityObjectPropertyValue("m_Script").AsFileID();
            if (fileID != null && fileID.IsExternal && fileID.IsMonoScript)
            {
                var typeElement = GetTypeElementFromScriptAssetGuid(componentDocument.GetSolution(), fileID.guid);
                if (typeElement != null)
                {
                    // TODO: Format like in Unity, by splitting the camel humps
                    return typeElement.ShortName + " (Script)";
                }
            }

            return scriptDocument.GetUnityObjectTypeFromRootNode()
                   ?? componentDocument.GetUnityObjectTypeFromRootNode()
                   ?? "Component";
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
        public static ITypeElement GetTypeElementFromScriptAssetGuid(ISolution solution, [CanBeNull] string assetGuid)
        {
            if (assetGuid == null)
                return null;

            var cache = solution.GetComponent<MetaFileGuidCache>();
            var assetPaths = cache.GetAssetFilePathsFromGuid(assetGuid);
            if (assetPaths == null || assetPaths.IsEmpty())
                return null;

            // TODO: Multiple candidates!
            // I.e. someone has copy/pasted a .meta file
            if (assetPaths.Count != 1)
                return null;

            var projectItems = solution.FindProjectItemsByLocation(assetPaths[0]);
            var assetFile = projectItems.FirstOrDefault() as IProjectFile;
            if (!(assetFile?.GetPrimaryPsiFile() is ICSharpFile csharpFile))
                return null;

            var expectedClassName = assetPaths[0].NameWithoutExtension;
            var psiSourceFile = csharpFile.GetSourceFile();
            if (psiSourceFile == null)
                return null;

            var psiServices = csharpFile.GetPsiServices();
            var elements = psiServices.Symbols.GetTypesAndNamespacesInFile(psiSourceFile);
            foreach (var element in elements)
            {
                // Note that theoretically, there could be multiple classes with the same name in different namespaces.
                // Unity's own behaviour here is undefined - it arbitrarily chooses one
                // TODO: Multiple candidates in a file
                if (element is ITypeElement typeElement && typeElement.ShortName == expectedClassName)
                    return typeElement;
            }

            return null;
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