#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Collections;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Serialization;
using JetBrains.Text;
using JetBrains.Util;
using JetBrains.Util.Collections;
using JetBrains.Util.dataStructures;
using JetBrains.Util.Maths;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches
{
    public static class AssetUtils
    {
        private static readonly StringSearcher ourMonoBehaviourCheck = new("!u!114 ", true);
        private static readonly StringSearcher ourFileIdCheck = new("fileID:", false);
        private static readonly StringSearcher ourPrefabModificationSearcher = new("!u!1001 ", true);
        private static readonly StringSearcher ourTransformSearcher = new("!u!4 ", true);
        private static readonly StringSearcher ourRectTransformSearcher = new("!u!224 ", true);
        private static readonly StringSearcher ourGameObjectSearcher = new("!u!1 ", true);
        private static readonly StringSearcher ourAnimatorStateSearcher = new("!u!1102", true);
        private static readonly StringSearcher ourAnimatorStateMachineSearcher = new("!u!1107", true);
        private static readonly StringSearcher ourAnimatorSearcher = new("!u!95 ", true);
        private static readonly StringSearcher ourStrippedSearcher = new(" stripped", true);
        private static readonly StringSearcher ourGameObjectFieldSearcher = new("m_GameObject:", true);
        private static readonly StringSearcher ourActionsSearcher = new("m_Actions:", true);
        private static readonly StringSearcher ourMotionSearcher = new("m_Motion:", true);
        private static readonly StringSearcher ourGameObjectNameSearcher = new("m_Name:", true);
        private static readonly StringSearcher ourRootOrderSearcher = new("m_RootOrder:", true);
        private static readonly StringSearcher ourPrefabInstanceSearcher = new("m_PrefabInstance:", true);
        private static readonly StringSearcher ourPrefabInstanceSearcher2017 = new("m_PrefabInternal:", true);
        private static readonly StringSearcher ourCorrespondingObjectSearcher = new("m_CorrespondingSourceObject:", true);
        private static readonly StringSearcher ourCorrespondingObjectSearcher2017 = new("m_PrefabParentObject:", true);
        private static readonly StringSearcher ourSourcePrefabSearcher = new("m_SourcePrefab:", true);
        private static readonly StringSearcher ourSourcePrefab2017Searcher = new("m_ParentPrefab:", true);
        private static readonly StringSearcher ourFatherSearcher = new("m_Father:", true);
        private static readonly StringSearcher ourChildrenSearcher = new("m_Children:", true);
        private static readonly StringSearcher ourChildSearcher = new("-", true);
        private static readonly StringSearcher ourBracketSearcher = new("}", true);
        private static readonly StringSearcher ourEndLineSearcher = new("\n", true);
        private static readonly StringSearcher ourEndLine2Searcher = new("\r", true);
        private static readonly StringSearcher ourColumnSearcher = new(":", true);

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

        public static bool IsAnimator(IBuffer buffer) =>
            ourAnimatorSearcher.Find(buffer, 0, Math.Min(buffer.Length, 30)) >= 0;

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
            while (index != buffer.Length && (char.IsDigit(buffer[index]) || buffer[index] == '-'))
            {
                sb.Append(buffer[index++]);
            }

            var resultStr = sb.ToString();
            if (long.TryParse(resultStr, out var result))
                return result;

            return null;
        }


        public static IHierarchyReference? GetGameObjectReference(IPsiSourceFile assetSourceFile, IBuffer assetDocumentBuffer) =>
            GetReferenceBySearcher(assetSourceFile, assetDocumentBuffer, ourGameObjectFieldSearcher);

        public static IHierarchyReference? GetInputActionsReference(IPsiSourceFile assetSourceFile, IBuffer assetDocumentBuffer) =>
            GetReferenceBySearcher(assetSourceFile, assetDocumentBuffer, ourActionsSearcher);

        public static IHierarchyReference? GetAnimReference(IPsiSourceFile assetSourceFile, IBuffer assetDocumentBuffer) =>
            GetReferenceBySearcher(assetSourceFile, assetDocumentBuffer, ourMotionSearcher);

        public static IHierarchyReference? GetTransformFather(IPsiSourceFile assetSourceFile, IBuffer assetDocumentBuffer) =>
            GetReferenceBySearcher(assetSourceFile, assetDocumentBuffer, ourFatherSearcher);

        public static IHierarchyReference? GetSourcePrefab(IPsiSourceFile assetSourceFile, IBuffer assetDocumentBuffer) =>
            GetReferenceBySearcher(assetSourceFile, assetDocumentBuffer, ourSourcePrefabSearcher) ??
            GetReferenceBySearcher(assetSourceFile, assetDocumentBuffer, ourSourcePrefab2017Searcher);

        public static int GetRootOrder(IBuffer assetDocumentBuffer)
        {
            var start = ourRootOrderSearcher.Find(assetDocumentBuffer, 0, assetDocumentBuffer.Length);
            if (start < 0)
                return 0;
            start += "m_RootOrder:".Length;
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
                if (assetDocumentBuffer[start] == '-' || char.IsDigit(assetDocumentBuffer[start])) // "-1" is possible
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
        
        public static FrugalLocalList<long> GetChildren(IPsiSourceFile assetSourceFile, IBuffer buffer)
        {
            var start = ourChildrenSearcher.Find(buffer, 0, buffer.Length);
            if (start < 0)
                return new FrugalLocalList<long>();
            start += "m_Children:".Length;
            
            // one possibility:
            //   m_Children: []
            // other possibility: 
            //   m_Children:
            //   - {fileID: 191985400}
            //   - {fileID: 1435803377}
            
            while (start < buffer.Length)
            {
                if (buffer[start].IsPureWhitespace())
                    start++;
                else if (buffer[start] == '[')
                    return new FrugalLocalList<long>();
                else
                    break;
            }
            
            var headerLineEnd = FindEndOfLine(buffer, start);
            if (headerLineEnd < 0)
                return new FrugalLocalList<long>();
            start = headerLineEnd + 1;

            var results = new FrugalLocalList<long>();
            var pos = start;
            while (pos < buffer.Length)
            {
                if (buffer[pos].IsPureWhitespace())
                {
                    pos++;
                    continue;
                }

                if (buffer[pos] == '-')
                {
                    var eol = FindEndOfLine(buffer, pos);
                    
                    var childBuffer = ProjectedBuffer.Create(buffer, new TextRange(pos + 1, eol));
                    var lexer = new YamlLexer(childBuffer, false, false);
                    var parser = new YamlParser(lexer.ToCachingLexer());
                    var document = parser.ParseDocument();
                    
                    if (document.Body.BlockNode is not IFlowMappingNode flowMappingNode) continue;
                    var localDocumentAnchor = flowMappingNode.GetMapEntryPlainScalarText("fileID");
                    if (localDocumentAnchor != null && long.TryParse(localDocumentAnchor, out var result))
                        results.Add(result);
                    
                    pos = eol + 1;
                    continue;
                }

                break;
            }

            return results;
        }

        private static int FindEndOfLine(IBuffer buffer, int start)
        {
            var eol = ourEndLineSearcher.Find(buffer, start, buffer.Length);
            if (eol < 0)
                eol = ourEndLine2Searcher.Find(buffer, start, buffer.Length);
            return eol;
        }

        public static string? GetGameObjectName(IBuffer buffer)
        {
            return GetPlainScalarValue(buffer, ourGameObjectNameSearcher);
        }

        public static string? GetPlainScalarValue(IBuffer buffer, StringSearcher searcher)
        {
            var start = searcher.Find(buffer, 0, buffer.Length);
            if (start < 0)
                return null;

            var eol = FindEndOfLine(buffer, start);
            if (eol < 0)
                return null;

            var nameBuffer = ProjectedBuffer.Create(buffer, new TextRange(start, eol + 1));
            var lexer = new YamlLexer(nameBuffer, false, false);
            var parser = new YamlParser(lexer.ToCachingLexer());
            var document = parser.ParseDocument();

            return (document.Body.BlockNode as IBlockMappingNode)?.Entries.FirstOrDefault()?.Content.Value
                .GetPlainScalarText();
        }

        public static IHierarchyReference? GetPrefabInstance(IPsiSourceFile assetSourceFile, IBuffer assetDocumentBuffer) =>
            GetReferenceBySearcher(assetSourceFile, assetDocumentBuffer, ourPrefabInstanceSearcher) ??
            GetReferenceBySearcher(assetSourceFile, assetDocumentBuffer, ourPrefabInstanceSearcher2017);

        public static IHierarchyReference? GetCorrespondingSourceObject(IPsiSourceFile assetSourceFile, IBuffer assetDocumentBuffer) =>
            GetReferenceBySearcher(assetSourceFile, assetDocumentBuffer, ourCorrespondingObjectSearcher) ??
            GetReferenceBySearcher(assetSourceFile, assetDocumentBuffer, ourCorrespondingObjectSearcher2017);

        public static IHierarchyReference? GetReferenceBySearcher(IPsiSourceFile assetSourceFile, IBuffer assetDocumentBuffer, StringSearcher searcher)
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

        public static IBlockMappingNode? GetPrefabModification(IYamlDocument yamlDocument)
        {
            // Prefab instance has a map of modifications, that stores delta of instance and prefab
            return yamlDocument.GetUnityObjectPropertyValue<IBlockMappingNode>(UnityYamlConstants.ModificationProperty);
        }

        public static IEnumerable<string> GetAllNamesFor(IField field)
        {
            yield return field.ShortName;

            foreach (var attribute in field.GetAttributeInstances(KnownTypes.FormerlySerializedAsAttribute, false))
            {
                var constantValue = attribute.PositionParameters().FirstOrDefault()?.ConstantValue;
                if (constantValue == null) continue;
                if (!constantValue.IsString(out var stringValue)) continue;
                if (stringValue == null) continue;
                yield return stringValue;
            }
        }

        public static string? GetRawComponentName(IBuffer assetDocumentBuffer)
        {
            var pos = ourColumnSearcher.Find(assetDocumentBuffer);
            if (pos < 0)
                return null;

            var startPos = pos;
            while (startPos >= 0)
            {
                if (assetDocumentBuffer[startPos] == '\r')
                    break;
                if (assetDocumentBuffer[startPos] == '\n')
                    break;

                startPos--;
            }

            return assetDocumentBuffer.GetText(new TextRange(startPos + 1, pos));
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

        public static ITypeElement? GetTypeElementFromScriptAssetGuid(ISolution solution, Guid? assetGuid)
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

                // this might be problematic - RIDER-87515 Support multiple MonoBehaviors classes in file
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
                    if (id.HasValue && service.UsageChecker is { } checker)
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

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once IdentifierTypo
        public static void WriteOWORD(OWORD value, UnsafeWriter unsafeWriter)
        {
            unsafeWriter.Write(value.loqword);
            unsafeWriter.Write(value.hiqword);
        }

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once IdentifierTypo
        public static OWORD ReadOWORD(UnsafeReader unsafeReader)
        {
            return new OWORD(unsafeReader.ReadULong(), unsafeReader.ReadULong());
        }
    }
}
