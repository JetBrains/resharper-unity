using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.dataStructures;
using Lex;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    // TODO [Krasnotsvetov] all comments is out-of-date and incorrect
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

        public static FrugalLocalList<T> ConstructGameObjectPath<T>([NotNull] IYamlDocument startGameObject, Func<IYamlDocument, bool, T> selector)
        {
            var result = new FrugalLocalList<T>();
            var currentGameObject = startGameObject;
            do
            {
                // current gameobject can be
                // 1. GameObject which has reference to prefab instance
                // 2. GameObject which is prefab instance
                // 3. GameObject
                // For 1st case, we should check that m_PrefabInstance is not null and has id greater than 0
                // For 2nd case, we should check that m_Modification exists
                // Otherwise, 3rd case
                //
                // 1st case do not contain actual data and has reference to 2nd case
                // 2nd case contain delta in m_modification (include m_Name)
                // 3rd case contain all actual data
               
                
                bool isPrefab = false;
                string fileId = currentGameObject.GetUnityObjectPropertyValue("m_PrefabInstance")?.AsFileID()?.fileID;
                if (fileId != null && !fileId.Equals("0"))
                {
                    isPrefab = true;
                    currentGameObject = currentGameObject.GetUnityObjectDocumentFromFileIDProperty("m_PrefabInstance");
                }
                else if (currentGameObject.GetUnityObjectPropertyValue("m_Modification") != null)
                {
                    isPrefab = true;
                }
                
                result.Add(selector(currentGameObject, isPrefab));
                currentGameObject = isPrefab ? GetParentFromPrefab(currentGameObject) : GetParentGameObject(currentGameObject);
            } while (currentGameObject != null);

            return result;
        }


        [NotNull]
        public static string GetGameObjectPath([NotNull] IYamlDocument componentDocument)
        {
            var gameObjectDocument = componentDocument.GetUnityObjectDocumentFromFileIDProperty("m_GameObject");
            if (gameObjectDocument == null) // It should never happen, this method calls from component IYamlDocument.
                return "INVALID";           // Each component has owner and owner must be gameobject (inside prefab too)

            var parts = ConstructGameObjectPath(gameObjectDocument, (document, isPrefab) =>
            {
                // ReSharper disable once ConvertToLambdaExpression
                return isPrefab ? ExtractNameFromPrefab(document) : document.GetUnityObjectPropertyValue("m_Name").AsString();
            });

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

        private static IBlockMappingNode GetPrefabModification(IYamlDocument yamlDocument)
        {
            // Prefab instance has a map of modifications, that stores delta of instance and prefab
            var prefabInstanceMap = (yamlDocument.BlockNode as IBlockMappingNode)
                ?.FindMapEntryBySimpleKey("PrefabInstance")?.Value as IBlockMappingNode;

            return prefabInstanceMap?.FindMapEntryBySimpleKey("m_Modification")?.Value as IBlockMappingNode;
        }
        
        public static IYamlDocument GetParentFromPrefab(IYamlDocument prefabInstanceDocument)
        {
            // Prefab instance stores it's father in modification map
            var prefabModification = GetPrefabModification(prefabInstanceDocument);

            var fileID = prefabModification?.FindMapEntryBySimpleKey("m_TransformParent")?.Value.AsFileID();
            if (fileID == null)
                return null;
            
            var file = (IYamlFile) prefabInstanceDocument.GetContainingFile();
            return GetGameObjectFromTransform(file.FindDocumentByAnchor(fileID.fileID));
        }

        public static string ExtractNameFromPrefab(IYamlDocument prefabInstanceDocument)
        {
            // Prefab instance stores it's name in mofidications sequence which belongs to modification map
            var prefabModifications = GetPrefabModification(prefabInstanceDocument)?.FindMapEntryBySimpleKey("m_Modifications")?.Value as IBlockSequenceNode;
            if (prefabModifications == null)
                return null;
                    
            var nameEntry = prefabModifications.Entries.FirstOrDefault(
                    t => ((t.Value as IBlockMappingNode)?.FindMapEntryBySimpleKey("propertyPath")?.Value as IPlainScalarNode)
                         ?.Text.GetText().Equals("m_Name") == true)?.Value as IBlockMappingNode;
                
                    
           
            return (nameEntry?.FindMapEntryBySimpleKey("value")?.Value as IPlainScalarNode)?.Text.GetText();
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
                        if (componentName != null && (componentName.Equals("RectTransform") || componentName.Equals("Transform")))
                            return component;
                    }
                }
            }

            return null;
        }

        [CanBeNull]
        public static IYamlDocument GetParentGameObject([CanBeNull] IYamlDocument gameObjectDocument)
        {
            var transformDocument = FindTransformComponentForGameObject(gameObjectDocument);
            
            // Transform contains information about scene hierarchy (m_Children, m_Father)
            var parentTransformDocument = transformDocument?.GetUnityObjectDocumentFromFileIDProperty("m_Father");
            return GetGameObjectFromTransform(parentTransformDocument);
        }

        public static IYamlDocument GetGameObjectFromTransform([CanBeNull] IYamlDocument yamlDocument)
        {
            if (yamlDocument == null)
                return null;
            
            // If transform belongs to gameObject it will contain m_GameObject
            // If transform belongs to prefabInstance it will contain m_PrefabInstance
             return yamlDocument.GetUnityObjectDocumentFromFileIDProperty("m_GameObject") ??
                    yamlDocument.GetUnityObjectDocumentFromFileIDProperty("m_PrefabInstance");
        }
    }
}