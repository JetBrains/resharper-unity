using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
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
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents
{
    [SolutionComponent]
    public class UnityEventsElementContainer : IUnityAssetDataElementContainer
    {
        private readonly IShellLocks myShellLocks;
        private readonly AssetDocumentHierarchyElementContainer myAssetDocumentHierarchyElementContainer;

        public UnityEventsElementContainer(IShellLocks shellLocks, AssetDocumentHierarchyElementContainer elementContainer)
        {
            myShellLocks = shellLocks;
            myAssetDocumentHierarchyElementContainer = elementContainer;
        }
        
        private static readonly StringSearcher ourMethodNameSearcher = new StringSearcher("m_MethodName", false);
        private readonly Dictionary<IPsiSourceFile, OneToListMap<string, AssetMethodData>> myPsiSourceFileToMethods = new Dictionary<IPsiSourceFile, OneToListMap<string, AssetMethodData>>();
        
        private readonly OneToCompactCountingSet<string, AssetMethodData> myLocalUsages = new OneToCompactCountingSet<string, AssetMethodData>();
        private readonly OneToCompactCountingSet<string, IPsiSourceFile> myFilesWithUsages = new OneToCompactCountingSet<string, IPsiSourceFile>();
        private readonly OneToCompactCountingSet<string, IPsiSourceFile> myFilesWithPossibleUsages = new OneToCompactCountingSet<string, IPsiSourceFile>();
        
        /// <summary>
        /// Prefab modification could add usage to method without using its name, e.g changing m_Target field in m_Calls array element or m_Mode.
        /// If file has m_Target modification and does not have any m_MethodName modification, we will add it to that counting set and
        /// for each find usages request for method we will scan all files from that set for usages
        ///
        /// Possible improvment:
        /// 1. Store for each IPsiSourceFile collection of m_Target references
        /// 2. GetPossibleFilesWithUsage will resolve m_Target to real ScriptComponentHierarchy with m_Script guid
        /// 3. GetPossibleFilesWithUsage will check that assosiated with guid type element is derived from method's type element and process only in that case
        /// NB : resolve to real ScriptComponentHierarchy will be cached in PrefabImportCache for stripped elements or will be simply availble in current scene hierarchy
        /// </summary>
        private readonly CountingSet<IPsiSourceFile> myFilesToCheckForUsages = new CountingSet<IPsiSourceFile>();
        private readonly CountingSet<string> myUnityEventsWithModifications = new CountingSet<string>();
        private readonly Dictionary<IPsiSourceFile, ImportedUnityEventData> myImportedUnityEventDatas = new Dictionary<IPsiSourceFile, ImportedUnityEventData>();
        private readonly Dictionary<(LocalReference, string), UnityEventData> myUnityEventDatas = new Dictionary<(LocalReference, string), UnityEventData>();

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
                    var calls = GetCalls(currentSourceFile, assetDocument, mCalls, location, eventTypeName);
                    
                    result.Add(new UnityEventData(name, location, scriptReference.Value, calls.ToArray()));
                }
            }

            return new UnityEventsBuildResult(modifications, result);
        }

        private ExternalReference? GetScriptReference(IPsiSourceFile sourceFile, TreeNodeCollection<IBlockMappingEntry> entries)
        {
            return entries.FirstOrDefault(t => "m_Script".Equals(t.Key.GetPlainScalarText()))?.Content.Value.AsFileID()?.ToReference(sourceFile) as ExternalReference?;
        }

        private LocalList<AssetMethodData> GetCalls(IPsiSourceFile currentSourceFile, AssetDocument assetDocument, IBlockSequenceNode mCalls, LocalReference location, string eventTypeName)
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
                result.Add(new AssetMethodData(location, methodName, range, currentSourceFile.PsiStorage.PersistentIndex, argMode, type, target));
            }

            return result;
        }

        private ImportedUnityEventData ProcessPrefabModifications(IPsiSourceFile currentFile, AssetDocument document)
        {
            var result = new ImportedUnityEventData();
            if (document.HierarchyElement is IPrefabInstanceHierarchy prefabInstanceHierarchy)
            {
                foreach (var modification in prefabInstanceHierarchy.PrefabModifications)
                {

                    if (!(modification.Target is ExternalReference externalReference))
                        continue;
                    
                    if (!modification.PropertyPath.Contains("m_PersistentCalls"))
                        continue;
                    
                    var location = new LocalReference(currentFile.PsiStorage.PersistentIndex, PrefabsUtil.Import(prefabInstanceHierarchy.Location.LocalDocumentAnchor, externalReference.LocalDocumentAnchor));
                    var parts = modification.PropertyPath.Split('.');
                    var unityEventName = parts[0];

                    var dataPart = parts.FirstOrDefault(t => t.StartsWith("data"));
                    if (dataPart == null)
                        continue;
                    
                    if (!int.TryParse(dataPart.RemoveStart("data[").RemoveEnd("]"), out var index))
                        continue;

                    var last = parts.Last();


                    var reference = new ImportedAssetMethodReference(location, unityEventName, index);
                    if (!result.ReferenceToImportedData.TryGetValue(reference, out var modifications))
                    {
                        modifications = new Dictionary<string, IAssetValue>();
                        result.ReferenceToImportedData[reference] = modifications;
                    }

                    switch (last)
                    {
                        case "m_Mode" when modification.Value is AssetSimpleValue simpleValue:
                            modifications[last] = simpleValue;
                            break;
                        case "m_MethodName" when modification.Value is AssetSimpleValue simpleValue:
                            modifications[last] = simpleValue;
                            modifications["m_MethodNameRange"] = new Int2Value(modification.ValueRange.StartOffset, modification.ValueRange.EndOffset);
                            break;
                        case "m_Target" when modification.ObjectReference is IHierarchyReference objectReference:
                            modifications[last] = new AssetReferenceValue(objectReference);
                            break;
                    }
                }
            }
        
            return result;
        }
        

        public void Drop(IPsiSourceFile sourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElement unityAssetDataElement)
        {
            var element = unityAssetDataElement as UnityEventsDataElement;
            foreach (var unityEventData in element.UnityEvents)
            {
                myUnityEventDatas.Remove((unityEventData.Location, unityEventData.Name));
                foreach (var call in unityEventData.Calls)
                {
                    myFilesWithUsages.Remove(call.MethodName, sourceFile); 
                
                    if (call.TargetScriptReference is ExternalReference)
                    {
                        myFilesWithPossibleUsages.Remove(call.MethodName, sourceFile);
                    } else if (call.TargetScriptReference is LocalReference localReference)
                    {
                        var scriptElement = assetDocumentHierarchyElement.GetHierarchyElement(null, localReference.LocalDocumentAnchor, null);
                        if (scriptElement is IScriptComponentHierarchy script)
                        {
                            myLocalUsages.Remove(call.MethodName, new AssetMethodData(LocalReference.Null, call.MethodName, TextRange.InvalidRange, 0,
                                call.Mode, call.Type, script.ScriptReference));
                        }
                        else
                        {
                            myFilesWithPossibleUsages.Remove(call.MethodName, sourceFile);
                        }
                    }
                }
            }
            
            foreach (var (reference, modification) in element.ImportedUnityEventData.ReferenceToImportedData)
            {
                if (modification.ContainsKey("m_Target") && !modification.ContainsKey("m_MethodName"))
                    myFilesToCheckForUsages.Remove(sourceFile);

                if (modification.TryGetValue("m_MethodName", out var name))
                    myFilesWithPossibleUsages.Remove((name as AssetSimpleValue).NotNull("name as AssetSimpleValue != null").SimpleValue, sourceFile);

                myUnityEventsWithModifications.Remove(reference.UnityEventName);
            }
            
            myPsiSourceFileToMethods.Remove(sourceFile);
            myImportedUnityEventDatas.Remove(sourceFile);
        }

        public void Merge(IPsiSourceFile sourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElementPointer unityAssetDataElementPointer, IUnityAssetDataElement unityAssetDataElement)
        {
            var element = (unityAssetDataElement as UnityEventsDataElement).NotNull("element != null");
            var groupMethods = new OneToListMap<string, AssetMethodData>();

            foreach (var unityEventData in element.UnityEvents)
            {
                myUnityEventDatas[(unityEventData.Location, unityEventData.Name)] = unityEventData;
                foreach (var method in unityEventData.Calls)
                {
                    myFilesWithUsages.Add(method.MethodName, sourceFile);
                    groupMethods.Add(method.MethodName, method);

                    if (method.TargetScriptReference is ExternalReference)
                    {
                        myFilesWithPossibleUsages.Add(method.MethodName, sourceFile);
                    }
                    else if (method.TargetScriptReference is LocalReference localReference)
                    {
                        var scriptElement = assetDocumentHierarchyElement.GetHierarchyElement(null, localReference.LocalDocumentAnchor, null);
                        if (scriptElement is IScriptComponentHierarchy script)
                        {
                            // only for fast resolve & counter
                            myLocalUsages.Add(method.MethodName, new AssetMethodData(LocalReference.Null, method.MethodName, TextRange.InvalidRange, 
                                0, method.Mode, method.Type, script.ScriptReference));
                        }
                        else
                        {
                            myFilesWithPossibleUsages.Add(method.MethodName, sourceFile);
                        }
                    }
                }
            }

            foreach (var (reference, modification) in element.ImportedUnityEventData.ReferenceToImportedData)
            {
                if (!modification.ContainsKey("m_MethodName"))
                    myFilesToCheckForUsages.Add(sourceFile);
                
                if (modification.TryGetValue("m_MethodName", out var name))
                    myFilesWithPossibleUsages.Add((name as AssetSimpleValue).NotNull("name as AssetSimpleValue != null").SimpleValue, sourceFile);

                myUnityEventsWithModifications.Add(reference.UnityEventName);
            }
            
            myPsiSourceFileToMethods.Add(sourceFile, groupMethods);
            myImportedUnityEventDatas[sourceFile] = element.ImportedUnityEventData;
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
            return myFilesWithUsages.GetValues(name).Length > 0 ||
                   myFilesWithPossibleUsages.GetValues(name).Length > 0;
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
            
            if (myFilesWithPossibleUsages.GetOrEmpty(declaredElement.ShortName).Count > 0)
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

        public IEnumerable<AssetMethodData> GetAssetUsagesFor(IPsiSourceFile psiSourceFile, IDeclaredElement declaredElement)
        {
            myShellLocks.AssertReadAccessAllowed();
            
            var result = new List<AssetMethodData>();
            foreach (var methodData in GetAssetMethodDataFor(psiSourceFile, declaredElement))
            {
                var symbolTable = GetReferenceSymbolTable(psiSourceFile.GetSolution(), psiSourceFile.GetPsiModule(), methodData, GetScriptGuid(methodData));
                var resolveResult = symbolTable.GetResolveResult(methodData.MethodName);
                if (resolveResult.ResolveErrorType == ResolveErrorType.OK && Equals(resolveResult.DeclaredElement, declaredElement))
                {
                    result.Add(methodData);
                }
            }
            
            return result;
        }


        private IEnumerable<AssetMethodData> GetAssetMethodDataFor(IPsiSourceFile psiSourceFile, IDeclaredElement declaredElement)
        {
            if (myPsiSourceFileToMethods.TryGetValue(psiSourceFile, out var methods))
            {
                foreach (var data in methods.GetValuesSafe(declaredElement.ShortName))
                    yield return data;
            }

            if (myImportedUnityEventDatas.TryGetValue(psiSourceFile, out var importedUnityEventData))
            {
                foreach (var (reference, modifications) in importedUnityEventData.ReferenceToImportedData)
                {
                    var element = myAssetDocumentHierarchyElementContainer.GetHierarchyElement(reference.Location, true);
                    Assertion.Assert(element is ImportedScriptComponentHierarchy, "element is ImportedScriptComponentHierarchy");
                    var script = element as ImportedScriptComponentHierarchy;
                    var originLocation = script.OriginLocation;
                    if (!myUnityEventDatas.TryGetValue((originLocation, reference.UnityEventName), out var unityEventData))
                        continue;

                    if (reference.MethodIndex < unityEventData.Calls.Count)
                    {
                        var import = unityEventData.Calls[reference.MethodIndex].Import(reference, modifications);
                        if (import != null)
                            yield return import;
                    }
                    else
                    {
                        var result = AssetMethodData.TryCreateAssetMethodFromModifications(reference, modifications);
                        if (result != null)
                            yield return result;
                    }
                }
            }
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
            myFilesToCheckForUsages.Clear();
            myFilesWithUsages.Clear();
            myFilesWithPossibleUsages.Clear();
            myPsiSourceFileToMethods.Clear();
            myLocalUsages.Clear();
        }

        public LocalList<IPsiSourceFile> GetPossibleFilesWithUsage(IDeclaredElement element)
        {
            if (element == null)
                return new LocalList<IPsiSourceFile>();

            var shortName = element.ShortName;

            var result = new LocalList<IPsiSourceFile>();
            foreach (var sourceFile in myFilesWithUsages.GetValues(shortName))
                result.Add(sourceFile);
            
            foreach (var sourceFile in myFilesWithPossibleUsages.GetValues(shortName))
                result.Add(sourceFile);

            // Name was mentioned as event handler in assets. We should
            // check each asset which has prefab modification with only m_Target.
            // see comment for collection
            if (result.Count > 0)
                foreach (var sourceFile in myFilesToCheckForUsages.GetItems())
                    result.Add(sourceFile);

            return result;
        }
    }
}