using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    public static class UnityObjectPsiUtil
    {
        [NotNull]
        public static string GetComponentName([NotNull] IYamlDocument componentDocument)
        {
            var name = componentDocument.GetUnityObjectPropertyValue(UnityYamlConstants.NameProperty).AsString();
            if (!string.IsNullOrWhiteSpace(name))
                return name;

            var scriptDocument = componentDocument.GetUnityObjectDocumentFromFileIDProperty(UnityYamlConstants.ScriptProperty);
            name = scriptDocument.GetUnityObjectPropertyValue(UnityYamlConstants.NameProperty).AsString();
            if (!string.IsNullOrWhiteSpace(name))
                return name;

            var fileID = componentDocument.GetUnityObjectPropertyValue(UnityYamlConstants.ScriptProperty).AsFileID();
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

        /// <summary>
        /// This method return path from component's owner to scene or prefab hierarchy root
        /// </summary>
        /// <param name="componentDocument">GameObject's component</param>
        /// <returns></returns>
        [NotNull]
        public static string GetGameObjectPathFromComponent([NotNull] UnitySceneProcessor sceneProcessor, [NotNull] IYamlDocument componentDocument)
        {
            var consumer = new UnityPathSceneConsumer();
            sceneProcessor.ProcessSceneHierarchyFromComponentToRoot(componentDocument, consumer);

            var parts = consumer.NameParts;

            if (parts.Count == 0)
                return "Unknown";
            
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

        public static IBlockMappingNode GetPrefabModification(IYamlDocument yamlDocument)
        {
            // Prefab instance has a map of modifications, that stores delta of instance and prefab
            return yamlDocument.GetUnityObjectPropertyValue(UnityYamlConstants.ModificationProperty) as IBlockMappingNode;
        }
        
        public static IYamlDocument GetTransformFromPrefabInstance(IYamlDocument prefabInstanceDocument)
        {
            // Prefab instance stores it's father in modification map
            var prefabModification = GetPrefabModification(prefabInstanceDocument);

            var fileID = prefabModification?.FindMapEntryBySimpleKey(UnityYamlConstants.TransformParentProperty)?.Value.AsFileID();
            if (fileID == null)
                return null;

            var file = (IYamlFile) prefabInstanceDocument.GetContainingFile();
            return file.FindDocumentByAnchor(fileID.fileID);
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
            var expectedClassName = assetPaths[0].NameWithoutExtension;
            var psiSourceFiles = assetFile?.ToSourceFiles();
            if (psiSourceFiles == null)
                return null;

            var psiServices = solution.GetPsiServices();
            foreach (var sourceFile in psiSourceFiles)
            {
                var elements = psiServices.Symbols.GetTypesAndNamespacesInFile(sourceFile);
                foreach (var element in elements)
                {
                    // Note that theoretically, there could be multiple classes with the same name in different
                    // namespaces. Unity's own behaviour here is undefined - it arbitrarily chooses one
                    // TODO: Multiple candidates in a file
                    if (element is ITypeElement typeElement && typeElement.ShortName == expectedClassName)
                        return typeElement;
                }
            }

            return null;
        }

        [CanBeNull]
        public static IYamlDocument FindTransformComponentForGameObject([CanBeNull] IYamlDocument gameObjectDocument)
        {
            // GameObject:
            //   m_Component:
            //   - component: {fileID: 1234567890}
            //   - component: {fileID: 1234567890}
            //   - component: {fileID: 1234567890}
            // One of these components is the RectTransform(GUI, 2D) or Transform(3D). Most likely the first, but we can't rely on order
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
                        var componentName = component.GetUnityObjectTypeFromRootNode();
                        if (componentName != null && (componentName.Equals(UnityYamlConstants.RectTransformComponent) || componentName.Equals(UnityYamlConstants.TransformComponent)))
                            return component;
                    }
                }
            }

            return null;
        }

        public static string GetValueFromModifications(IBlockMappingNode modification, string targetFileId, string value)
        {
            if (targetFileId != null && modification.FindMapEntryBySimpleKey(UnityYamlConstants.ModificationsProperty)?.Value is IBlockSequenceNode modifications)
            {
                foreach (var element in modifications.Entries)
                {
                    if (!(element.Value is IBlockMappingNode mod))
                        return null;
                    var type = (mod.FindMapEntryBySimpleKey(UnityYamlConstants.PropertyPathProperty)?.Value as IPlainScalarNode)
                        ?.Text.GetText();
                    var target = mod.FindMapEntryBySimpleKey(UnityYamlConstants.TargetProperty)?.Value?.AsFileID();
                    if (type?.Equals(value) == true && target?.fileID.Equals(targetFileId) == true)
                    {
                        return (mod.FindMapEntryBySimpleKey(UnityYamlConstants.ValueProperty)?.Value as IPlainScalarNode)?.Text.GetText();
                    }
                }
            }

            return null;
        }
    }
}