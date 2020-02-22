using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.JavaScript.Util.Literals;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches
{
    public class AssetUtils
    {
        private static readonly StringSearcher ourMonoBehaviourCheck = new StringSearcher("!u!114 ", true);
        private static readonly StringSearcher ourFileIdCheck = new StringSearcher("fileID:", false);
        private static readonly StringSearcher ourPrefabModificationSearcher = new StringSearcher("!u!1001 ", true);
        private static readonly StringSearcher ourTransformSearcher = new StringSearcher("!u!4 ", true);
        private static readonly StringSearcher ourRectTransformSearcher = new StringSearcher("!u!224 ", true);
        private static readonly StringSearcher ourGameObjectSearcher = new StringSearcher("!u!1 ", true);
        private static readonly StringSearcher ourStrippedSearcher = new StringSearcher(" stripped", true);
        private static readonly StringSearcher ourGameObjectFieldSearcher = new StringSearcher("m_GameObject:", true);
        private static readonly StringSearcher ourGameObjectNameSearcher = new StringSearcher("m_Name:", true);
        private static readonly StringSearcher ourRootIndexSearcher = new StringSearcher("m_RootOrder:", true);
        private static readonly StringSearcher ourPrefabInstanceSearcher = new StringSearcher("m_PrefabInstance:", true);
        private static readonly StringSearcher ourPrefabInstanceSearcher2017 = new StringSearcher("m_PrefabInternal:", true);
        private static readonly StringSearcher ourCorrespondingObjectSearcher = new StringSearcher("m_CorrespondingSourceObject:", true);
        private static readonly StringSearcher ourCorrespondingObjectSearcher2017 = new StringSearcher("m_PrefabParentObject:", true);
        private static readonly StringSearcher ourSourcePrefabSearcher = new StringSearcher("m_SourcePrefab:", true);
        private static readonly StringSearcher ourFatherSearcher = new StringSearcher("m_Father:", true);
        private static readonly StringSearcher ourBracketSearcher = new StringSearcher("}", true);
        private static readonly StringSearcher ourEndLineSearcher = new StringSearcher("\n", true);
        private static readonly StringSearcher ourEndLine2Searcher = new StringSearcher("\r", true);
        private static readonly StringSearcher ourColumnSearcher = new StringSearcher(":", true);

        public static bool IsMonoBehaviourDocument(IBuffer buffer) =>
            ourMonoBehaviourCheck.Find(buffer, 0, Math.Min(buffer.Length, 20)) >= 0;

        public static bool IsReferenceValue(IBuffer buffer) =>
            ourFileIdCheck.Find(buffer, 0, Math.Min(buffer.Length, 30)) >= 0;
        
        public static bool IsPrefabModification(IBuffer buffer) =>
            ourPrefabModificationSearcher.Find(buffer, 0, Math.Min(buffer.Length, 30)) >= 0;
        
        public static bool IsTransform(IBuffer buffer) =>
            ourTransformSearcher.Find(buffer, 0, Math.Min(buffer.Length, 30)) >= 0 ||
            ourRectTransformSearcher.Find(buffer, 0, Math.Min(buffer.Length, 30)) >= 0;
        
        public static bool IsGameObject(IBuffer buffer) =>
            ourGameObjectSearcher.Find(buffer, 0, Math.Min(buffer.Length, 30)) >= 0;
        
        public static bool IsStripped(IBuffer buffer) =>
            ourStrippedSearcher.Find(buffer, 0, Math.Min(buffer.Length, 150)) >= 0;
        
        public static ulong? GetAnchorFromBuffer(IBuffer buffer)
        {
            var index = 0;
            while (true)
            {
                if (index == buffer.Length)
                    return null;
                
                if (buffer[index] == '&')
                    break;

                index++;
            }
            index++;

            var sb = new StringBuilder();
            while (index != buffer.Length && buffer[index].IsDigit())
            {
                sb.Append(buffer[index++]);
            }

            var resultStr = sb.ToString();
            if (ulong.TryParse(resultStr, out var result))
                return result;

            return null;
        }


        [CanBeNull]
        public static AssetDocumentReference GetGameObject(IBuffer assetDocumentBuffer) =>
            GetReferenceBySearcher(assetDocumentBuffer, ourGameObjectFieldSearcher);
        
        [CanBeNull]
        public static AssetDocumentReference GetTransformFather(IBuffer assetDocumentBuffer) =>
            GetReferenceBySearcher(assetDocumentBuffer, ourFatherSearcher);
        
        [CanBeNull]
        public static AssetDocumentReference GetSourcePrefab(IBuffer assetDocumentBuffer) =>
            GetReferenceBySearcher(assetDocumentBuffer, ourSourcePrefabSearcher);

        public static int GetRootIndex(IBuffer assetDocumentBuffer)
        {
            var start = ourRootIndexSearcher.Find(assetDocumentBuffer, 0, assetDocumentBuffer.Length);
            if (start < 0)
                return 0;
            start += "m_RootIndex:".Length;
            while (start < assetDocumentBuffer.Length)
            {
                if (assetDocumentBuffer[start].IsPureWhitespace())
                    start++;
                else
                    break;
            }
            
            var result = new StringBuilder();

            while (start < assetDocumentBuffer.Length)
            {
                if (assetDocumentBuffer[start].IsDigit())
                {
                    result.Append(assetDocumentBuffer[start]);
                    start++;
                }
                else
                {
                    break;
                }
            }

            return int.TryParse(result.ToString(), out var index) ? index : 0;
        }

        [CanBeNull]
        public static string GetGameObjectName(IBuffer buffer)
        {
            var start = ourGameObjectNameSearcher.Find(buffer, 0, buffer.Length);
            if (start < 0)
                return null;

            var eol = ourEndLineSearcher.Find(buffer, start, buffer.Length);
            if (eol < 0)
                eol = ourEndLine2Searcher.Find(buffer, start, buffer.Length);
            if (eol < 0)
                return null;
            
            var nameBuffer = ProjectedBuffer.Create(buffer, new TextRange(start, eol + 1));
            var lexer = new YamlLexer(nameBuffer, false, false);
            var parser = new YamlParser(lexer.ToCachingLexer());
            var document = parser.ParseDocument();

            return (document.Body.BlockNode as IBlockMappingNode)?.Entries.FirstOrDefault()?.Content.Value
                .GetPlainScalarText();
        }
        
        [CanBeNull]
        public static AssetDocumentReference GetPrefabInstance(IBuffer assetDocumentBuffer) =>
            GetReferenceBySearcher(assetDocumentBuffer, ourPrefabInstanceSearcher) ??
            GetReferenceBySearcher(assetDocumentBuffer, ourPrefabInstanceSearcher2017);

        [CanBeNull]
        public static AssetDocumentReference GetCorrespondingSourceObject(IBuffer assetDocumentBuffer) =>
            GetReferenceBySearcher(assetDocumentBuffer, ourCorrespondingObjectSearcher) ??
            GetReferenceBySearcher(assetDocumentBuffer, ourCorrespondingObjectSearcher2017);
        
        [CanBeNull]
        public static AssetDocumentReference GetReferenceBySearcher(IBuffer assetDocumentBuffer, StringSearcher searcher)
        {
            var start = searcher.Find(assetDocumentBuffer, 0, assetDocumentBuffer.Length);
            if (start < 0)
                return null;
            var end = ourBracketSearcher.Find(assetDocumentBuffer, start, assetDocumentBuffer.Length);
            if (end < 0)
                return null;
            
            var buffer = ProjectedBuffer.Create(assetDocumentBuffer, new TextRange(start, end + 1));
            var lexer = new YamlLexer(buffer, false, false);
            var parser = new YamlParser(lexer.ToCachingLexer());
            var document = parser.ParseDocument();

            return (document.Body.BlockNode as IBlockMappingNode)?.Entries.FirstOrDefault()?.Content.Value.AsFileID();
        }
        
        [CanBeNull]
        public static IBlockMappingNode GetPrefabModification(IYamlDocument yamlDocument)
        {
            // Prefab instance has a map of modifications, that stores delta of instance and prefab
            return yamlDocument.GetUnityObjectPropertyValue(UnityYamlConstants.ModificationProperty) as IBlockMappingNode;
        }

        private static readonly IClrTypeName ourFormerlySerializedAsAttribute = new ClrTypeName("UnityEngine.Serialization.FormerlySerializedAsAttribute");
        public static IEnumerable<string> GetAllNamesFor(IField field)
        {
            yield return field.ShortName;

            foreach (var attribute in field.GetAttributeInstances(ourFormerlySerializedAsAttribute, false))
            {
                var result = attribute.PositionParameters().FirstOrDefault()?.ConstantValue.Value as string;
                if (result == null)
                    continue;
                yield return result;
            }
        }

        public static string GetRawComponentName(IBuffer assetDocumentBuffer)
        {
            var pos = ourColumnSearcher.Find(assetDocumentBuffer);
            if (pos < 0)
                return null;

            pos--;
            var sb = new StringBuilder();
            while (pos >= 0)
            {
                if (pos == '\r')
                    break;
                if (pos == '\n')
                    break;

                sb.Append(assetDocumentBuffer[pos]);
                pos--;
            }

            return sb.ToString();
        }
        
        public static string GetComponentName(MetaFileGuidCache metaFileGuidCache, IComponentHierarchy componentHierarchy)
        {
            if (componentHierarchy is IScriptComponentHierarchy scriptComponent)
            {
                var result = metaFileGuidCache.GetAssetNames(scriptComponent.ScriptReference.ExternalAssetGuid).FirstOrDefault();
                if (result != null)
                    return result;
            }

            return componentHierarchy.Name;
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
    }
}