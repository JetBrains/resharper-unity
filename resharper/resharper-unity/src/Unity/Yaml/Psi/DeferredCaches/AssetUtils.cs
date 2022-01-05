using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Collections;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.JavaScript.Util.Literals;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Serialization;
using JetBrains.Text;
using JetBrains.Util;
using JetBrains.Util.Collections;
using JetBrains.Util.Maths;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches
{
    public static class AssetUtils
    {
        private static readonly StringSearcher ourMonoBehaviourCheck = new StringSearcher("!u!114 ", true);
        private static readonly StringSearcher ourFileIdCheck = new StringSearcher("fileID:", false);
        private static readonly StringSearcher ourPrefabModificationSearcher = new StringSearcher("!u!1001 ", true);
        private static readonly StringSearcher ourTransformSearcher = new StringSearcher("!u!4 ", true);
        private static readonly StringSearcher ourRectTransformSearcher = new StringSearcher("!u!224 ", true);
        private static readonly StringSearcher ourGameObjectSearcher = new StringSearcher("!u!1 ", true);
        private static readonly StringSearcher ourAnimatorStateSearcher = new StringSearcher("!u!1102", true);
        private static readonly StringSearcher ourAnimatorStateMachineSearcher = new StringSearcher("!u!1107", true);
        private static readonly StringSearcher ourStrippedSearcher = new StringSearcher(" stripped", true);
        private static readonly StringSearcher ourGameObjectFieldSearcher = new StringSearcher("m_GameObject:", true);
        private static readonly StringSearcher ourGameObjectNameSearcher = new StringSearcher("m_Name:", true);
        private static readonly StringSearcher ourRootIndexSearcher = new StringSearcher("m_RootOrder:", true);
        private static readonly StringSearcher ourPrefabInstanceSearcher = new StringSearcher("m_PrefabInstance:", true);
        private static readonly StringSearcher ourPrefabInstanceSearcher2017 = new StringSearcher("m_PrefabInternal:", true);
        private static readonly StringSearcher ourCorrespondingObjectSearcher = new StringSearcher("m_CorrespondingSourceObject:", true);
        private static readonly StringSearcher ourCorrespondingObjectSearcher2017 = new StringSearcher("m_PrefabParentObject:", true);
        private static readonly StringSearcher ourSourcePrefabSearcher = new StringSearcher("m_SourcePrefab:", true);
        private static readonly StringSearcher ourSourcePrefab2017Searcher = new StringSearcher("m_ParentPrefab:", true);
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

        public static bool IsAnimatorState(IBuffer buffer) =>
            ourAnimatorStateSearcher.Find(buffer, 0, Math.Min(buffer.Length, 30)) >= 0;

        public static bool IsAnimatorStateMachine(IBuffer buffer) =>
            ourAnimatorStateMachineSearcher.Find(buffer, 0, Math.Min(buffer.Length, 30)) >= 0;

        public static long? GetAnchorFromBuffer(IBuffer buffer)
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
            while (index != buffer.Length && (buffer[index].IsDigit() || buffer[index] == '-'))
            {
                sb.Append(buffer[index++]);
            }

            var resultStr = sb.ToString();
            if (long.TryParse(resultStr, out var result))
                return result;

            return null;
        }


        [CanBeNull]
        public static IHierarchyReference GetGameObjectReference(IPsiSourceFile assetSourceFile, IBuffer assetDocumentBuffer) =>
            GetReferenceBySearcher(assetSourceFile, assetDocumentBuffer, ourGameObjectFieldSearcher);

        [CanBeNull]
        public static IHierarchyReference GetTransformFather(IPsiSourceFile assetSourceFile, IBuffer assetDocumentBuffer) =>
            GetReferenceBySearcher(assetSourceFile, assetDocumentBuffer, ourFatherSearcher);

        [CanBeNull]
        public static IHierarchyReference GetSourcePrefab(IPsiSourceFile assetSourceFile, IBuffer assetDocumentBuffer) =>
            GetReferenceBySearcher(assetSourceFile, assetDocumentBuffer, ourSourcePrefabSearcher) ??
            GetReferenceBySearcher(assetSourceFile, assetDocumentBuffer, ourSourcePrefab2017Searcher);

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
        public static IHierarchyReference GetPrefabInstance(IPsiSourceFile assetSourceFile, IBuffer assetDocumentBuffer) =>
            GetReferenceBySearcher(assetSourceFile, assetDocumentBuffer, ourPrefabInstanceSearcher) ??
            GetReferenceBySearcher(assetSourceFile, assetDocumentBuffer, ourPrefabInstanceSearcher2017);

        [CanBeNull]
        public static IHierarchyReference GetCorrespondingSourceObject(IPsiSourceFile assetSourceFile, IBuffer assetDocumentBuffer) =>
            GetReferenceBySearcher(assetSourceFile, assetDocumentBuffer, ourCorrespondingObjectSearcher) ??
            GetReferenceBySearcher(assetSourceFile, assetDocumentBuffer, ourCorrespondingObjectSearcher2017);

        [CanBeNull]
        public static IHierarchyReference GetReferenceBySearcher(IPsiSourceFile assetSourceFile, IBuffer assetDocumentBuffer, StringSearcher searcher)
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

            return (document.Body.BlockNode as IBlockMappingNode)?.Entries.FirstOrDefault()?.Content.Value.ToHierarchyReference(assetSourceFile);
        }

        [CanBeNull]
        public static IBlockMappingNode GetPrefabModification(IYamlDocument yamlDocument)
        {
            // Prefab instance has a map of modifications, that stores delta of instance and prefab
            return yamlDocument.GetUnityObjectPropertyValue<IBlockMappingNode>(UnityYamlConstants.ModificationProperty);
        }

        public static IEnumerable<string> GetAllNamesFor(IField field)
        {
            yield return field.ShortName;

            foreach (var attribute in field.GetAttributeInstances(KnownTypes.FormerlySerializedAsAttribute, false))
            {
                var result = attribute.PositionParameters().FirstOrDefault()?.ConstantValue.Value as string;
                if (result == null)
                    continue;
                yield return result;
            }
        }

        public static string GetRawComponentName(IBuffer assetDocumentBuffer)
        {
            var pos = ourColumnSearcher.Find(assetDocumentBuffer) - 1;
            if (pos < 0)
                return null;

            var startPos = pos--;
            while (startPos >= 0)
            {
                if (assetDocumentBuffer[startPos] == '\r')
                    break;
                if (assetDocumentBuffer[startPos] == '\n')
                    break;

                startPos--;
            }

            return assetDocumentBuffer.GetText(new TextRange(startPos, pos));
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
        public static ITypeElement GetTypeElementFromScriptAssetGuid(ISolution solution, [CanBeNull] Guid? assetGuid)
        {
            if (assetGuid == null)
                return null;

            var cache = solution.GetComponent<MetaFileGuidCache>();
            var assetPaths = cache.GetAssetFilePathsFromGuid(assetGuid.Value);
            if (assetPaths == null || assetPaths.IsEmpty())
                return null;

            // TODO: Multiple candidates!
            // I.e. someone has copy/pasted a .meta file
            if (assetPaths.Count != 1)
                return null;

            var projectItems = solution.FindProjectItemsByLocation(assetPaths[0]).Where(t => !t.IsMiscProjectItem() && !t.GetProject().IsPlayerProject());
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

        public static Guid? GetGuidFor(MetaFileGuidCache metaFileGuidCache, ITypeElement typeElement)
        {
            // partial classes
            var declarations = typeElement.GetDeclarations();
            foreach (var declaration in declarations)
            {
                var sourceFile = declaration.GetSourceFile();
                if (sourceFile == null || !sourceFile.IsValid())
                    continue;

                if (!typeElement.ShortName.Equals(sourceFile.GetLocation().NameWithoutExtension))
                    continue;

                if (typeElement.TypeParameters.Count != 0)
                    continue;

                if (typeElement.GetContainingType() != null)
                    continue;

                var guid = metaFileGuidCache.GetAssetGuid(sourceFile);
                return guid;
            }

            return null;
        }

        public static bool HasPossibleDerivedTypesWithMember(Guid ownerGuid, ITypeElement containingType, IEnumerable<string> memberNames, OneToCompactCountingSet<int, Guid> nameHashToGuids)
        {

            var count = 0;
            foreach (var possibleName in memberNames)
            {
                var values = nameHashToGuids.GetValues(possibleName.GetPlatformIndependentHashCode());
                count += values.Length;
                if (values.Length == 1 && !values[0].Equals(ownerGuid))
                    count++;
            }

            if (count > 1)
            {
                // TODO: drop daemon dependency and inject components in constructor
                var configuration = containingType.GetSolution().GetComponent<SolutionAnalysisConfiguration>();
                if (configuration.Enabled.Value && configuration.CompletedOnceAfterStart.Value &&
                    configuration.Loaded.Value)
                {
                    var service = containingType.GetSolution().GetComponent<SolutionAnalysisService>();
                    var id = service.GetElementId(containingType);
                    if (id.HasValue && service.UsageChecker is IGlobalUsageChecker checker)
                    {
                        // no inheritors
                        if (checker.GetDerivedTypeElementsCount(id.Value) == 0)
                            return false;
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Source dictionary will be changed!
        /// </summary>
        /// <param name="source"></param>
        /// <param name="import"></param>
        /// <returns></returns>
        public static Dictionary<string, IAssetValue> Import(Dictionary<string, IAssetValue> source, Dictionary<string, IAssetValue> import)
        {
            foreach (var (name, value) in import)
            {
                source[name] = value;
            }

            return source;
        }

        public static void WriteOWORD(OWORD oword, UnsafeWriter unsafeWriter)
        {
            unsafeWriter.Write(oword.loqword);
            unsafeWriter.Write(oword.hiqword);
        }

        public static OWORD ReadOWORD(UnsafeReader unsafeReader)
        {
            return new OWORD(unsafeReader.ReadULong(), unsafeReader.ReadULong());
        }
    }
}