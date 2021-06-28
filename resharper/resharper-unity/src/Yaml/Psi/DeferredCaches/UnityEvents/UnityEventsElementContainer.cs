using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Filters;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents
{
    // UnityEventsElementContainer stores whole index in memory, it is not expected to have millions of methods. In case of high memory usage
    // with millions of methods, only pointers to AssetMethodData should be stored. AssetMethodData will be deserialized only in find usages,
    // strings should be replaced by int hashes.
    // Information about imported/prefab modifications could be stored in memory, it should not allocate a lot of memory ever.
    [SolutionComponent]
    public partial class UnityEventsElementContainer : IUnityAssetDataElementContainer
    {
        private readonly ISolution mySolution;
        private readonly IShellLocks myShellLocks;
        private readonly MetaFileGuidCache myGuidCache;
        private readonly AssetDocumentHierarchyElementContainer myAssetDocumentHierarchyElementContainer;
        private readonly ILogger myLogger;

        public UnityEventsElementContainer(ISolution solution, IShellLocks shellLocks, MetaFileGuidCache guidCache,
            AssetDocumentHierarchyElementContainer elementContainer, ILogger logger)
        {
            mySolution = solution;
            myShellLocks = shellLocks;
            myGuidCache = guidCache;
            myAssetDocumentHierarchyElementContainer = elementContainer;
            myLogger = logger;
        }

        private static readonly StringSearcher ourMethodNameSearcher = new StringSearcher("m_MethodName", false);

        #region FIND_USAGES_METHODS

        private readonly OneToSetMap<IPsiSourceFile, UnityEventData> myPsiSourceFileToEventData = new OneToSetMap<IPsiSourceFile, UnityEventData>();
        private readonly OneToCompactCountingSet<string, IPsiSourceFile> myMethodNameToFilesWithUsages = new OneToCompactCountingSet<string, IPsiSourceFile>();

        // for counter, could be remove in case of memory optimization and replaced by low-priority background task, do not forget to
        // add files to `myFilesWithUsages` instead of `myFilesWithPossibleUsages`.
        private readonly OneToCompactCountingSet<string, IPsiSourceFile> myMethodNameToFilesWithPossibleUsages = new OneToCompactCountingSet<string, IPsiSourceFile>();
        private readonly OneToCompactCountingSet<string, AssetMethodUsages> myLocalMethodUsages = new OneToCompactCountingSet<string, AssetMethodUsages>();

        #endregion

        // Inspector values for unity event support
        #region UNITY_EVENTS_FIND_USAGES

        // for estimated counter check
        private readonly OneToCompactCountingSet<int, Guid> myUnityEventNameHashToScriptGuids = new OneToCompactCountingSet<int, Guid>();
        private readonly CountingSet<string> myUnityEventsWithModifications = new CountingSet<string>();
        private readonly Dictionary<(LocalReference, string), UnityEventData> myUnityEventDatas = new Dictionary<(LocalReference, string), UnityEventData>();

        // for count check & find usages
        private readonly CountingSet<(string eventName, Guid scriptOwner)> myUnityEventUsageCount = new CountingSet<(string eventName, Guid scriptOwner)>();
        private readonly OneToSetMap<string, IPsiSourceFile> myUnityEventNameToSourceFiles = new OneToSetMap<string, IPsiSourceFile>();

        #endregion

        public IUnityAssetDataElement CreateDataElement(IPsiSourceFile sourceFile)
        {
            return new UnityEventsDataElement();
        }

        public bool IsApplicable(IPsiSourceFile currentAssetSourceFile)
        {
            return true;
        }

        public object Build(SeldomInterruptChecker checker, IPsiSourceFile currentAssetSourceFile, AssetDocument assetDocument)
        {
            var modifications = ProcessPrefabModifications(currentAssetSourceFile, assetDocument);
            var buffer = assetDocument.Buffer;
            if (ourMethodNameSearcher.Find(buffer) < 0)
                return new UnityEventsBuildResult(modifications, new LocalList<UnityEventData>());

            var anchorRaw = AssetUtils.GetAnchorFromBuffer(assetDocument.Buffer);
            if (!anchorRaw.HasValue)
                return new UnityEventsBuildResult(modifications, new LocalList<UnityEventData>());

            var anchor = anchorRaw.Value;

            var properties = assetDocument.Document.GetUnityObjectProperties();
            var entries = properties?.Entries;
            if (entries == null)
                return new UnityEventsBuildResult(modifications, new LocalList<UnityEventData>());

            var scriptReference = properties.GetMapEntryValue<INode>(UnityYamlConstants.ScriptProperty)
                    .ToHierarchyReference(currentAssetSourceFile) as ExternalReference?;
            if (scriptReference == null)
                return new UnityEventsBuildResult(modifications, new LocalList<UnityEventData>());

            var location = new LocalReference(currentAssetSourceFile.PsiStorage.PersistentIndex.NotNull("owningPsiPersistentIndex != null"), anchor);

            var result = new LocalList<UnityEventData>();
            foreach (var entry in entries)
            {
                if (ourMethodNameSearcher.Find(entry.Content.GetTextAsBuffer()) >= 0)
                {
                    var name = entry.Key.GetPlainScalarText();
                    if (name == null)
                        continue;

                    var rootMap = entry.Content.Value as IBlockMappingNode;
                    var persistentCallsMap = rootMap.GetMapEntryValue<IBlockMappingNode>("m_PersistentCalls");
                    var mCalls = persistentCallsMap.GetMapEntryValue<IBlockSequenceNode>("m_Calls");
                    if (mCalls == null)
                        continue;

                    var eventTypeName = rootMap.GetMapEntryPlainScalarText("m_TypeName");
                    var calls = GetCalls(currentAssetSourceFile, assetDocument, mCalls, name, eventTypeName);

                    result.Add(new UnityEventData(name, location, scriptReference.Value, calls.ToArray()));
                }
            }

            return new UnityEventsBuildResult(modifications, result);
        }

        private LocalList<AssetMethodUsages> GetCalls(IPsiSourceFile currentAssetSourceFile,
                                                      AssetDocument assetDocument, IBlockSequenceNode mCalls,
                                                      string name, string eventTypeName)
        {
            var result = new LocalList<AssetMethodUsages>();
            foreach (var call in mCalls.Entries)
            {
                var methodDescription = call.Value as IBlockMappingNode;
                if (methodDescription == null)
                    continue;

                var target = methodDescription.GetMapEntryValue<INode>("m_Target")
                    .ToHierarchyReference(currentAssetSourceFile);
                if (target == null)
                    continue;

                var methodNameNode = methodDescription.GetMapEntryValue<INode>("m_MethodName");
                var methodName = methodNameNode?.GetPlainScalarText();
                if (methodName == null)
                    continue;

                var methodNameRange = methodNameNode.GetTreeTextRange();

                var arguments = methodDescription.GetMapEntryValue<IBlockMappingNode>("m_Arguments");
                var modeText = methodDescription.GetMapEntryPlainScalarText("m_Mode");
                var argMode = EventHandlerArgumentMode.EventDefined;
                if (int.TryParse(modeText, out var mode))
                {
                    if (1 <= mode && mode <= 6)
                        argMode = (EventHandlerArgumentMode)mode;
                }

                var argumentTypeName = arguments.GetMapEntryPlainScalarText("m_ObjectArgumentAssemblyTypeName");

                var type = argumentTypeName?.Split(',').FirstOrDefault();
                if (argMode == EventHandlerArgumentMode.EventDefined)
                    type = eventTypeName?.Split(',').FirstOrDefault();
                else if (argMode == EventHandlerArgumentMode.Void)
                    type = null;

                var range = new TextRange(assetDocument.StartOffset + methodNameRange.StartOffset.Offset,
                    assetDocument.StartOffset + methodNameRange.EndOffset.Offset);

                result.Add(new AssetMethodUsages(name, methodName, range,
                    currentAssetSourceFile.PsiStorage.PersistentIndex.NotNull("owningPsiPersistentIndex != null"),
                    argMode, type, target));
            }

            return result;
        }

        public void Drop(IPsiSourceFile currentAssetSourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElement unityAssetDataElement)
        {
            var element = unityAssetDataElement as UnityEventsDataElement;
            foreach (var unityEventData in element.UnityEvents)
            {
                var eventName = unityEventData.Name;
                var guid = unityEventData.ScriptReference.ExternalAssetGuid;
                myUnityEventNameHashToScriptGuids.Remove(eventName.GetPlatformIndependentHashCode(), guid);
                myUnityEventNameToSourceFiles.Remove(eventName, currentAssetSourceFile);
                myUnityEventUsageCount.Add((eventName, guid), -unityEventData.Calls.Count);

                myUnityEventDatas.Remove((unityEventData.OwningScriptLocation, unityEventData.Name));
                foreach (var call in unityEventData.Calls)
                {
                    myMethodNameToFilesWithUsages.Remove(call.MethodName, currentAssetSourceFile);

                    if (call.TargetScriptReference is ExternalReference)
                    {
                        myMethodNameToFilesWithPossibleUsages.Remove(call.MethodName, currentAssetSourceFile);
                    } else if (call.TargetScriptReference is LocalReference localReference)
                    {
                        var scriptElement = assetDocumentHierarchyElement.GetHierarchyElement(null, localReference.LocalDocumentAnchor, null);

                        if (scriptElement is IScriptComponentHierarchy script)
                        {
                            myLocalMethodUsages.Remove(call.MethodName, new AssetMethodUsages(unityEventData.Name, call.MethodName, TextRange.InvalidRange, 0,
                                call.Mode, call.Type, script.ScriptReference));
                        }
                        else
                        {
                            myMethodNameToFilesWithPossibleUsages.Remove(call.MethodName, currentAssetSourceFile);
                        }
                    }
                }
            }

            DropPrefabModifications(currentAssetSourceFile, element);

            myPsiSourceFileToEventData.RemoveKey(currentAssetSourceFile);
        }

        public void Merge(IPsiSourceFile currentAssetSourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElementPointer unityAssetDataElementPointer, IUnityAssetDataElement unityAssetDataElement)
        {
            var element = (unityAssetDataElement as UnityEventsDataElement).NotNull("element != null");

            foreach (var unityEventData in element.UnityEvents)
            {
                var eventName = unityEventData.Name;
                var guid = unityEventData.ScriptReference.ExternalAssetGuid;
                myUnityEventNameHashToScriptGuids.Add(eventName.GetPlatformIndependentHashCode(), guid);
                myUnityEventNameToSourceFiles.Add(eventName, currentAssetSourceFile);
                myUnityEventUsageCount.Add((eventName, guid), unityEventData.Calls.Count);
                myUnityEventDatas[(unityEventData.OwningScriptLocation, eventName)] = unityEventData;
                myPsiSourceFileToEventData.Add(currentAssetSourceFile, unityEventData);

                foreach (var method in unityEventData.Calls)
                {
                    myMethodNameToFilesWithUsages.Add(method.MethodName, currentAssetSourceFile);

                    if (method.TargetScriptReference is ExternalReference)
                    {
                        myMethodNameToFilesWithPossibleUsages.Add(method.MethodName, currentAssetSourceFile);
                    }
                    else if (method.TargetScriptReference is LocalReference localReference)
                    {
                        var scriptElement = assetDocumentHierarchyElement.GetHierarchyElement(null, localReference.LocalDocumentAnchor, null);

                        // If targetScriptReference points to scene's script, we could extract script guid and calculate buckets for each AssetMethodUsages
                        // For each bucket we need resolve only once and then we could increment counter for declared element by bucket size
                        if (scriptElement is IScriptComponentHierarchy script)
                        {
                            // only for fast resolve & counter
                            myLocalMethodUsages.Add(method.MethodName, new AssetMethodUsages(unityEventData.Name, method.MethodName, TextRange.InvalidRange,
                                0, method.Mode, method.Type, script.ScriptReference));
                        }
                        else
                        {
                            myMethodNameToFilesWithPossibleUsages.Add(method.MethodName, currentAssetSourceFile);
                        }
                    }
                }
            }

            MergePrefabModifications(currentAssetSourceFile, element);

        }

        private bool IsPossibleEventHandler(IDeclaredElement declaredElement)
        {
            myShellLocks.AssertReadAccessAllowed();

            if (declaredElement is IProperty property)
            {
                var setter = property.Setter;
                if (setter != null && IsPossibleEventHandler(setter.ShortName))
                    return true;

                return false;
            }

            return IsPossibleEventHandler(declaredElement.ShortName);
        }

        private bool IsPossibleEventHandler(string name)
        {
            return myMethodNameToFilesWithUsages.GetValues(name).Length > 0 ||
                   myMethodNameToFilesWithPossibleUsages.GetValues(name).Length > 0;
        }

        public int GetAssetUsagesCount(IDeclaredElement declaredElement, out bool estimatedResult)
        {
            if (declaredElement is IProperty property)
            {
                var getter = property.Getter;
                var setter = property.Setter;

                var count = 0;
                estimatedResult = false;
                if (getter != null)
                {
                    count += GetAssetUsagesCountInner(getter, out var getterEstimated);
                    estimatedResult |= getterEstimated;
                }

                if (setter != null)
                {
                    count += GetAssetUsagesCountInner(setter, out var setterEstimated);
                    estimatedResult |= setterEstimated;
                }

                return count;
            }

            return GetAssetUsagesCountInner(declaredElement, out estimatedResult);
        }

        private int GetAssetUsagesCountInner(IDeclaredElement declaredElement, out bool estimatedResult)
        {
            myShellLocks.AssertReadAccessAllowed();
            estimatedResult = false;
            if (!(declaredElement is IClrDeclaredElement clrDeclaredElement))
                return 0;

            if (!IsPossibleEventHandler(declaredElement))
                return 0;

            if (myMethodNameToFilesWithPossibleUsages.GetOrEmpty(declaredElement.ShortName).Count > 0)
                estimatedResult = true;

            const int maxProcessCount = 5;
            if (myLocalMethodUsages.GetOrEmpty(declaredElement.ShortName).Count > maxProcessCount)
                estimatedResult = true;

            var usageCount = 0;
            foreach (var (assetMethodData, c) in myLocalMethodUsages.GetOrEmpty(declaredElement.ShortName).Take(maxProcessCount))
            {
                var solution = declaredElement.GetSolution();
                var module = clrDeclaredElement.Module;

                // we have already cache guid in merge method for methodData in myLocalUsages
                var guid = (assetMethodData.TargetScriptReference as ExternalReference?)?.ExternalAssetGuid;
                if (guid == null)
                    continue;

                var symbolTable = GetReferenceSymbolTable(solution, module, assetMethodData, guid.Value);
                var resolveResult = symbolTable.GetResolveResult(assetMethodData.MethodName);
                if (resolveResult.ResolveErrorType == ResolveErrorType.OK && Equals(resolveResult.DeclaredElement, declaredElement))
                {
                    usageCount += c;
                }
            }

            return usageCount;
        }

        private Guid? GetScriptGuid(AssetMethodUsages assetMethodUsages)
        {
            var reference = assetMethodUsages.TargetScriptReference;
            var scriptComponent = myAssetDocumentHierarchyElementContainer.GetHierarchyElement(reference, true) as IScriptComponentHierarchy;
            var guid = scriptComponent?.ScriptReference.ExternalAssetGuid;

            return guid;
        }

        public IEnumerable<UnityEventHandlerFindResult> GetAssetUsagesFor(IPsiSourceFile psiSourceFile, IDeclaredElement declaredElement)
        {
            myShellLocks.AssertReadAccessAllowed();
            var result = new List<UnityEventHandlerFindResult>();
            foreach (var (owningScriptLocation, methodData, isPrefab) in GetAssetMethodDataFor(psiSourceFile))
            {
                var symbolTable = GetReferenceSymbolTable(psiSourceFile.GetSolution(), psiSourceFile.GetPsiModule(), methodData, GetScriptGuid(methodData));
                var resolveResult = symbolTable.GetResolveResult(methodData.MethodName);
                if (resolveResult.ResolveErrorType == ResolveErrorType.OK && Equals(resolveResult.DeclaredElement, declaredElement))
                {
                    result.Add(new UnityEventHandlerFindResult(psiSourceFile, declaredElement, methodData, owningScriptLocation, isPrefab));
                }
            }

            myLogger.Trace($"{psiSourceFile.Name} --> {result.Count} usage(s)");
            return result;
        }

        public int GetUsageCountForEvent(IField field, out bool isEstimated)
        {
            myShellLocks.AssertReadAccessAllowed();

            isEstimated = false;
            var containingType = field?.GetContainingType();
            if (containingType == null)
                return 0;

            var guid = AssetUtils.GetGuidFor(myGuidCache, containingType);
            if (guid == null)
                return 0;

            var result = 0;
            foreach (var name in AssetUtils.GetAllNamesFor(field))
            {
                result += myUnityEventUsageCount.GetCount((name, guid.Value));
            }

            if (myUnityEventsWithModifications.Contains(field.ShortName))
                isEstimated = true;

            if (!isEstimated)
                isEstimated = AssetUtils.HasPossibleDerivedTypesWithMember(guid.Value, containingType,
                    AssetUtils.GetAllNamesFor(field), myUnityEventNameHashToScriptGuids);

            return result;
        }

        public IEnumerable<UnityEventSubscriptionFindResult> GetMethodsForUnityEvent(IPsiSourceFile psiSourceFile, IField field)
        {
            myShellLocks.AssertReadAccessAllowed();

            var containingType = field?.GetContainingType();
            if (containingType == null)
                yield break;

            var guid = AssetUtils.GetGuidFor(myGuidCache, containingType);
            if (guid == null)
                yield break;

            var names = AssetUtils.GetAllNamesFor(field).ToJetHashSet();
            foreach (var (owningScriptLocation, method, isPrefab) in GetAssetMethodDataFor(psiSourceFile))
            {
                var ownerName = method.OwnerName;
                if (!names.Contains(ownerName))
                    continue;

                var scriptElement = myAssetDocumentHierarchyElementContainer.GetHierarchyElement(owningScriptLocation, true) as IScriptComponentHierarchy;
                Assertion.Assert(scriptElement != null, "scriptElement != null");
                if (scriptElement.ScriptReference.ExternalAssetGuid != guid)
                    continue;

                var symbolTable = GetReferenceSymbolTable(psiSourceFile.GetSolution(), psiSourceFile.GetPsiModule(), method, GetScriptGuid(method));
                var resolveResult = symbolTable.GetResolveResult(method.MethodName);
                if (resolveResult.ResolveErrorType == ResolveErrorType.OK)
                    yield return new UnityEventSubscriptionFindResult(resolveResult.DeclaredElement, psiSourceFile, owningScriptLocation, isPrefab);
            }
        }

        private IEnumerable<(LocalReference owningScriptLocation, AssetMethodUsages method, bool isPrefabModification)> GetAssetMethodDataFor(IPsiSourceFile psiSourceFile)
        {
            foreach (var data in myPsiSourceFileToEventData.GetValuesSafe(psiSourceFile))
                foreach (var call in data.Calls)
                    yield return (data.OwningScriptLocation, call, false);

            foreach (var result in GetImportedAssetMethodDataFor(psiSourceFile))
                yield return (result.owningScriptLocation, result.methodData, true);
        }

        private ISymbolTable GetReferenceSymbolTable(ISolution solution, IPsiModule psiModule, AssetMethodUsages assetMethodUsages, Guid? assetGuid)
        {
            var targetType = AssetUtils.GetTypeElementFromScriptAssetGuid(solution, assetGuid);
            if (targetType == null)
                return EmptySymbolTable.INSTANCE;

            var symbolTable = ResolveUtil.GetSymbolTableByTypeElement(targetType, SymbolTableMode.FULL, psiModule);

            return symbolTable.Filter(assetMethodUsages.MethodName, IsMethodFilter.INSTANCE, OverriddenFilter.INSTANCE, new ExactNameFilter(assetMethodUsages.MethodName),
                new StaticFilter(new NonStaticAccessContext(null)), new EventHandlerSymbolFilter(assetMethodUsages.Mode, assetMethodUsages.Type, targetType.Module));
        }

        public string Id => nameof(UnityEventsElementContainer);
        public int Order => 0;

        public void Invalidate()
        {
            myUnityEventUsageCount.Clear();
            myUnityEventDatas.Clear();
            myUnityEventsWithModifications.Clear();
            myUnityEventNameToSourceFiles.Clear();
            myUnityEventNameHashToScriptGuids.Clear();
            myImportedUnityEventDatas.Clear();
            myFilesToCheckForUsages.Clear();
            myMethodNameToFilesWithUsages.Clear();
            myMethodNameToFilesWithPossibleUsages.Clear();
            myPsiSourceFileToEventData.Clear();
        }

        public LocalList<IPsiSourceFile> GetPossibleFilesWithUsage(IDeclaredElement element)
        {
            if (element == null)
                return new LocalList<IPsiSourceFile>();

            var result = new LocalList<IPsiSourceFile>();
            var shortName = element.ShortName;

            // unity events
            if (element is IField field)
            {
                foreach (var name in AssetUtils.GetAllNamesFor(field))
                    foreach (var psiSourceFile in myUnityEventNameToSourceFiles.GetValuesSafe(name))
                        result.Add(psiSourceFile);

                return result;
            }

            foreach (var sourceFile in myMethodNameToFilesWithUsages.GetValues(shortName))
                result.Add(sourceFile);

            foreach (var sourceFile in myMethodNameToFilesWithPossibleUsages.GetValues(shortName))
                result.Add(sourceFile);

            // Name was mentioned as event handler in assets. We should
            // check each asset which has prefab modification with only m_Target.
            // see comment for collection
            if (result.Count > 0)
                foreach (var sourceFile in myFilesToCheckForUsages.GetItems())
                    result.Add(sourceFile);

            return result;
        }

        public IEnumerable<UnityEventData> GetUnityEventDataFor(LocalReference location, JetHashSet<string> allUnityEventNames)
        {
            foreach (var name in allUnityEventNames)
                if (myUnityEventDatas.TryGetValue((location, name), out var data))
                    yield return data;
        }
    }
}