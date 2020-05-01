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

        public UnityEventsElementContainer(ISolution solution, IShellLocks shellLocks, MetaFileGuidCache guidCache, AssetDocumentHierarchyElementContainer elementContainer)
        {
            mySolution = solution;
            myShellLocks = shellLocks;
            myGuidCache = guidCache;
            myAssetDocumentHierarchyElementContainer = elementContainer;
        }
        
        private static readonly StringSearcher ourMethodNameSearcher = new StringSearcher("m_MethodName", false);

        #region FIND_USAGES_METHODS
        
        private readonly OneToSetMap<IPsiSourceFile, UnityEventData> myPsiSourceFileToEventData = new OneToSetMap<IPsiSourceFile, UnityEventData>();
        private readonly OneToCompactCountingSet<string, IPsiSourceFile> myMethodNameToFilesWithUsages = new OneToCompactCountingSet<string, IPsiSourceFile>();
        
        // for counter, could be remove in case of memory optimization and replaced by low-priority background task, do not forget to
        // add files to `myFilesWithUsages` instead of `myFilesWithPossibleUsages`.
        private readonly OneToCompactCountingSet<string, IPsiSourceFile> myMethodNameToFilesWithPossibleUsages = new OneToCompactCountingSet<string, IPsiSourceFile>();
        private readonly OneToCompactCountingSet<string, AssetMethodData> myLocalUsages = new OneToCompactCountingSet<string, AssetMethodData>();

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
            return new UnityEventsDataElement(sourceFile);
        }

        public object Build(SeldomInterruptChecker checker, IPsiSourceFile currentSourceFile, AssetDocument assetDocument)
        {
            var modifications = ProcessPrefabModifications(currentSourceFile, assetDocument);
            var buffer = assetDocument.Buffer;
            if (ourMethodNameSearcher.Find(buffer) < 0)
                return new UnityEventsBuildResult(modifications, new LocalList<UnityEventData>());

            var anchorRaw = AssetUtils.GetAnchorFromBuffer(assetDocument.Buffer);
            if (!anchorRaw.HasValue)
                return new UnityEventsBuildResult(modifications, new LocalList<UnityEventData>());

            var anchor = anchorRaw.Value;
            
            var entries = assetDocument.Document.FindRootBlockMapEntries()?.Entries;
            if (entries == null)
                return new UnityEventsBuildResult(modifications, new LocalList<UnityEventData>());


            var location = new LocalReference(currentSourceFile.PsiStorage.PersistentIndex, anchor);
            var scriptReference = GetScriptReference(currentSourceFile, entries.Value);
            if (scriptReference == null)
                return new UnityEventsBuildResult(modifications, new LocalList<UnityEventData>());
            
            var result = new LocalList<UnityEventData>();
            foreach (var entry in entries)
            {
                if (ourMethodNameSearcher.Find(entry.Content.GetTextAsBuffer()) >= 0)
                {
                    var name = entry.Key.GetPlainScalarText();
                    if (name == null) 
                        continue;
                    
                    var rootMap = entry.Content.Value as IBlockMappingNode;
                    var persistentCallsMap = rootMap.GetValue("m_PersistentCalls") as IBlockMappingNode;
                    var mCalls = persistentCallsMap.GetValue("m_Calls") as IBlockSequenceNode;
                    if (mCalls == null)
                        continue;
                    
                    var eventTypeName = rootMap.GetValue("m_TypeName").GetPlainScalarText();
                    var calls = GetCalls(currentSourceFile, assetDocument, mCalls, location, name, eventTypeName);
                    
                    result.Add(new UnityEventData(name, location, scriptReference.Value, calls.ToArray()));
                }
            }

            return new UnityEventsBuildResult(modifications, result);
        }

        private ExternalReference? GetScriptReference(IPsiSourceFile sourceFile, TreeNodeCollection<IBlockMappingEntry> entries)
        {
            return entries.FirstOrDefault(t => "m_Script".Equals(t.Key.GetPlainScalarText()))?.Content.Value.AsFileID()?.ToReference(sourceFile) as ExternalReference?;
        }

        private LocalList<AssetMethodData> GetCalls(IPsiSourceFile currentSourceFile, AssetDocument assetDocument, IBlockSequenceNode mCalls, LocalReference location, string name, string eventTypeName)
        {
            var result = new LocalList<AssetMethodData>();
            foreach (var call in mCalls.Entries)
            {
                var methodDescription = call.Value as IBlockMappingNode;
                if (methodDescription == null)
                    continue;
                
                var fileID = methodDescription.FindMapEntryBySimpleKey("m_Target")?.Content.Value.AsFileID();
                if (fileID == null)
                    continue;

                var methodNameNode = methodDescription.GetValue("m_MethodName");
                var methodName = methodNameNode?.GetPlainScalarText();
                if (methodName == null)
                    continue;

                var methodNameRange = methodNameNode.GetTreeTextRange();
                
                var arguments = methodDescription.GetValue("m_Arguments") as IBlockMappingNode;
                var modeText = methodDescription.GetValue("m_Mode")?.GetPlainScalarText();
                var argMode = EventHandlerArgumentMode.EventDefined;
                if (int.TryParse(modeText, out var mode))
                {
                    if (1 <= mode && mode <= 6)
                        argMode = (EventHandlerArgumentMode) mode;
                }
                
                var argumentTypeName = arguments.GetValue("m_ObjectArgumentAssemblyTypeName")?.GetPlainScalarText();
                
                var type = argumentTypeName?.Split(',').FirstOrDefault();
                if (argMode == EventHandlerArgumentMode.EventDefined)
                    type = eventTypeName?.Split(',').FirstOrDefault();
                else if (argMode == EventHandlerArgumentMode.Void)
                    type = null;

                var range = new TextRange(assetDocument.StartOffset + methodNameRange.StartOffset.Offset,
                    assetDocument.StartOffset + methodNameRange.EndOffset.Offset);

                var target = fileID.ToReference(currentSourceFile);
                result.Add(new AssetMethodData(location, name, methodName, range, currentSourceFile.PsiStorage.PersistentIndex, argMode, type, target));
            }

            return result;
        }
        
        public void Drop(IPsiSourceFile sourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElement unityAssetDataElement)
        {
            var element = unityAssetDataElement as UnityEventsDataElement;
            foreach (var unityEventData in element.UnityEvents)
            {
                var eventName = unityEventData.Name;
                var guid = unityEventData.ScriptReference.ExternalAssetGuid;
                myUnityEventNameHashToScriptGuids.Remove(eventName.GetPlatformIndependentHashCode(), guid);
                myUnityEventNameToSourceFiles.Remove(eventName, sourceFile);
                myUnityEventUsageCount.Add((eventName, guid), -unityEventData.Calls.Count);
                    
                myUnityEventDatas.Remove((unityEventData.Location, unityEventData.Name));
                foreach (var call in unityEventData.Calls)
                {
                    myMethodNameToFilesWithUsages.Remove(call.MethodName, sourceFile); 
                
                    if (call.TargetScriptReference is ExternalReference)
                    {
                        myMethodNameToFilesWithPossibleUsages.Remove(call.MethodName, sourceFile);
                    } else if (call.TargetScriptReference is LocalReference localReference)
                    {
                        var scriptElement = assetDocumentHierarchyElement.GetHierarchyElement(null, localReference.LocalDocumentAnchor, null);
                        if (scriptElement is IScriptComponentHierarchy script)
                        {
                            myLocalUsages.Remove(call.MethodName, new AssetMethodData(LocalReference.Null, unityEventData.Name, call.MethodName, TextRange.InvalidRange, 0,
                                call.Mode, call.Type, script.ScriptReference));
                        }
                        else
                        {
                            myMethodNameToFilesWithPossibleUsages.Remove(call.MethodName, sourceFile);
                        }
                    }
                }
            }

            DropPrefabModifications(sourceFile, assetDocumentHierarchyElement, element);

            myPsiSourceFileToEventData.RemoveKey(sourceFile);
        }

        public void Merge(IPsiSourceFile sourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElementPointer unityAssetDataElementPointer, IUnityAssetDataElement unityAssetDataElement)
        {
            var element = (unityAssetDataElement as UnityEventsDataElement).NotNull("element != null");

            foreach (var unityEventData in element.UnityEvents)
            {
                var eventName = unityEventData.Name;
                var guid = unityEventData.ScriptReference.ExternalAssetGuid;
                myUnityEventNameHashToScriptGuids.Add(eventName.GetPlatformIndependentHashCode(), guid);
                myUnityEventNameToSourceFiles.Add(eventName, sourceFile);
                myUnityEventUsageCount.Add((eventName, guid), unityEventData.Calls.Count);
                myUnityEventDatas[(unityEventData.Location, eventName)] = unityEventData;
                myPsiSourceFileToEventData.Add(sourceFile, unityEventData);

                foreach (var method in unityEventData.Calls)
                {
                    myMethodNameToFilesWithUsages.Add(method.MethodName, sourceFile);

                    if (method.TargetScriptReference is ExternalReference)
                    {
                        myMethodNameToFilesWithPossibleUsages.Add(method.MethodName, sourceFile);
                    }
                    else if (method.TargetScriptReference is LocalReference localReference)
                    {
                        var scriptElement = assetDocumentHierarchyElement.GetHierarchyElement(null, localReference.LocalDocumentAnchor, null);
                        if (scriptElement is IScriptComponentHierarchy script)
                        {
                            // only for fast resolve & counter
                            myLocalUsages.Add(method.MethodName, new AssetMethodData(LocalReference.Null, unityEventData.Name, method.MethodName, TextRange.InvalidRange, 
                                0, method.Mode, method.Type, script.ScriptReference));
                        }
                        else
                        {
                            myMethodNameToFilesWithPossibleUsages.Add(method.MethodName, sourceFile);
                        }
                    }
                }
            }

            MergePrefabModifications(sourceFile, assetDocumentHierarchyElement, unityAssetDataElementPointer, element);

        }

        private bool IsPossibleEventHandler(IDeclaredElement declaredElement)
        {
            myShellLocks.AssertReadAccessAllowed();
            
            if (declaredElement is IProperty property)
            {
                var setter = property.Setter;
                if (setter != null && IsPossibleEventHandler(setter.ShortName))
                    return true;
                    
                var getter = property.Getter;
                if (getter != null && IsPossibleEventHandler(getter.ShortName))
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
            if (myLocalUsages.GetOrEmpty(declaredElement.ShortName).Count > maxProcessCount)
                estimatedResult = true;

            var usageCount = 0;
            foreach (var (assetMethodData, c) in myLocalUsages.GetOrEmpty(declaredElement.ShortName).Take(maxProcessCount))
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

        private Guid? GetScriptGuid(AssetMethodData assetMethodData)
        {
            var reference = assetMethodData.TargetScriptReference;
            var scriptComponent = myAssetDocumentHierarchyElementContainer.GetHierarchyElement(reference, true) as IScriptComponentHierarchy;
            var guid = scriptComponent?.ScriptReference.ExternalAssetGuid; 
            
            return guid;
        }

        public IEnumerable<UnityMethodsFindResult> GetAssetUsagesFor(IPsiSourceFile psiSourceFile, IDeclaredElement declaredElement)
        {
            myShellLocks.AssertReadAccessAllowed();
            
            var result = new List<UnityMethodsFindResult>();
            foreach (var (methodData, isPrefab) in GetAssetMethodDataFor(psiSourceFile))
            {
                var symbolTable = GetReferenceSymbolTable(psiSourceFile.GetSolution(), psiSourceFile.GetPsiModule(), methodData, GetScriptGuid(methodData));
                var resolveResult = symbolTable.GetResolveResult(methodData.MethodName);
                if (resolveResult.ResolveErrorType == ResolveErrorType.OK && Equals(resolveResult.DeclaredElement, declaredElement))
                {
                    result.Add(new UnityMethodsFindResult(psiSourceFile, declaredElement, methodData, methodData.OwnerLocation, isPrefab));
                }
            }
            
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
        
        public IEnumerable<UnityEventFindResult> GetMethodsForUnityEvent(IPsiSourceFile psiSourceFile, IField field)
        {
            myShellLocks.AssertReadAccessAllowed();

            var containingType = field?.GetContainingType();
            if (containingType == null)
                yield break;

            var guid = AssetUtils.GetGuidFor(myGuidCache, containingType);
            if (guid == null)
                yield break;

            var names = AssetUtils.GetAllNamesFor(field).ToJetHashSet();
            foreach (var (method, isPrefab) in GetAssetMethodDataFor(psiSourceFile))
            {
                var location = method.OwnerLocation;
                var ownerName = method.OwnerName;
                if (!names.Contains(ownerName))
                    continue;
                
                var scriptElement = myAssetDocumentHierarchyElementContainer.GetHierarchyElement(location, true) as IScriptComponentHierarchy;
                Assertion.Assert(scriptElement != null, "scriptElement != null");
                if (scriptElement.ScriptReference.ExternalAssetGuid != guid)
                    continue;
                
                var symbolTable = GetReferenceSymbolTable(psiSourceFile.GetSolution(), psiSourceFile.GetPsiModule(), method, GetScriptGuid(method));
                var resolveResult = symbolTable.GetResolveResult(method.MethodName);
                if (resolveResult.ResolveErrorType == ResolveErrorType.OK)
                    yield return new UnityEventFindResult(resolveResult.DeclaredElement, psiSourceFile, method.OwnerLocation, isPrefab);
            }
        }

        private IEnumerable<(AssetMethodData method, bool isPrefabModification)> GetAssetMethodDataFor(IPsiSourceFile psiSourceFile)
        {
            foreach (var data in myPsiSourceFileToEventData.GetValuesSafe(psiSourceFile))
                foreach (var call in data.Calls)
                    yield return (call, false);

            foreach (var result in GetImportedAssetMethodDataFor(psiSourceFile))
                yield return (result, true);
        }

        private ISymbolTable GetReferenceSymbolTable(ISolution solution, IPsiModule psiModule, AssetMethodData assetMethodData, Guid? assetGuid)
        {
            var targetType = AssetUtils.GetTypeElementFromScriptAssetGuid(solution, assetGuid);
            if (targetType == null)
                return EmptySymbolTable.INSTANCE;

            var symbolTable = ResolveUtil.GetSymbolTableByTypeElement(targetType, SymbolTableMode.FULL, psiModule);

            return symbolTable.Filter(assetMethodData.MethodName, IsMethodFilter.INSTANCE, OverriddenFilter.INSTANCE, new ExactNameFilter(assetMethodData.MethodName),
                new StaticFilter(new NonStaticAccessContext(null)), new EventHandlerSymbolFilter(assetMethodData.Mode, assetMethodData.Type, targetType.Module));
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