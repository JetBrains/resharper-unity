using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Filters;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.Collections;


namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetMethods
{
    [SolutionComponent]
    public class AssetMethodsElementContainer : IUnityAssetDataElementContainer
    {
        private readonly IShellLocks myShellLocks;
        private readonly ISolution mySolution;
        private readonly UnityInterningCache myUnityInterningCache;
        private readonly IPersistentIndexManager myPersistentIndexManager;
        private readonly AssetDocumentHierarchyElementContainer myAssetDocumentHierarchyElementContainer;

        public AssetMethodsElementContainer(IShellLocks shellLocks, ISolution solution, IPersistentIndexManager persistentIndexManager, UnityInterningCache unityInterningCache, AssetDocumentHierarchyElementContainer elementContainer)
        {
            myShellLocks = shellLocks;
            mySolution = solution;
            myUnityInterningCache = unityInterningCache;
            myPersistentIndexManager = persistentIndexManager;
            myAssetDocumentHierarchyElementContainer = elementContainer;
        }
        
        private static readonly StringSearcher ourMethodNameSearcher = new StringSearcher("m_MethodName", false);
        private readonly OneToCompactCountingSet<string, AssetMethodData> myShortNameToScriptTarget = new OneToCompactCountingSet<string, AssetMethodData>();
        private readonly Dictionary<IPsiSourceFile, OneToListMap<string, AssetMethodData>> myPsiSourceFileToMethods = new Dictionary<IPsiSourceFile, OneToListMap<string, AssetMethodData>>();
        
        private readonly CountingSet<string> myExternalCount = new CountingSet<string>();
        private readonly OneToCompactCountingSet<string, AssetMethodData> myLocalUsages = new OneToCompactCountingSet<string, AssetMethodData>();

        public IUnityAssetDataElement CreateDataElement(IPsiSourceFile sourceFile)
        {
            return new AssetMethodsDataElement(sourceFile);
        }

        public object Build(SeldomInterruptChecker checker, IPsiSourceFile currentSourceFile, AssetDocument assetDocument)
        {
            var result = new LocalList<AssetMethodData>();
            var buffer = assetDocument.Buffer;
            if (ourMethodNameSearcher.Find(buffer) < 0)
                return null;

            var anchorRaw = AssetUtils.GetAnchorFromBuffer(assetDocument.Buffer);
            if (!anchorRaw.HasValue)
                return null;

            var anchor = anchorRaw.Value;
            
            var entries = assetDocument.Document.FindRootBlockMapEntries()?.Entries;
            if (entries == null)
                return null;

            foreach (var entry in entries)
            {
                if (ourMethodNameSearcher.Find(entry.Content.GetTextAsBuffer()) >= 0)
                {
                    var rootMap = entry.Content.Value as IBlockMappingNode;
                    var persistentCallsMap = rootMap.GetValue("m_PersistentCalls") as IBlockMappingNode;
                    var mCalls = persistentCallsMap.GetValue("m_Calls") as IBlockSequenceNode;
                    if (mCalls == null)
                        return null;
                    
                    var eventTypeName = rootMap.GetValue("m_TypeName").GetPlainScalarText();
                    
                    foreach (var call in mCalls.Entries)
                    {
                        var methodDescription = call.Value as IBlockMappingNode;
                        if (methodDescription == null)
                            continue;
                        
                        var fileID = methodDescription.FindMapEntryBySimpleKey("m_Target")?.Content.Value.AsFileID();
                        if (fileID == null || fileID.IsNullReference)
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
                        if (target != null)
                        {
                            result.Add(new AssetMethodData(
                                new LocalReference(currentSourceFile.PsiStorage.PersistentIndex, anchor), methodName,
                                range,
                                argMode, type, target));
                        }
                    }
                }
            }

            if (result.Count > 0)
                return result;
            return null;
        }

        public void Drop(IPsiSourceFile sourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElement unityAssetDataElement)
        {
            var element = unityAssetDataElement as AssetMethodsDataElement;
            foreach (var method in element.Methods)
            {
                myShortNameToScriptTarget.Remove(method.MethodName, method);
                
                if (method.TargetScriptReference is ExternalReference)
                {
                    myExternalCount.Remove(method.MethodName);
                } else if (method.TargetScriptReference is LocalReference localReference)
                {
                    
                    var scriptElement = assetDocumentHierarchyElement.GetHierarchyElement(null, localReference.LocalDocumentAnchor, myUnityInterningCache, null);
                    if (scriptElement is IScriptComponentHierarchy script)
                    {
                        myLocalUsages.Remove(method.MethodName, new AssetMethodData(LocalReference.Null,
                            method.MethodName, TextRange.InvalidRange,
                            method.Mode, method.Type, script.GetScriptReference(myUnityInterningCache)));
                    }
                    else
                    {
                        myExternalCount.Remove(method.MethodName);
                    }
                }
            }

            myPsiSourceFileToMethods.Remove(sourceFile);
        }

        public void Merge(IPsiSourceFile sourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElementPointer unityAssetDataElementPointer, IUnityAssetDataElement unityAssetDataElement)
        {
            var element = (unityAssetDataElement as AssetMethodsDataElement).NotNull("element != null");
            var groupMethods = new OneToListMap<string, AssetMethodData>();
            foreach (var method in element.Methods)
            {
                myShortNameToScriptTarget.Add(method.MethodName, method);
                groupMethods.Add(method.MethodName, method);

                if (method.TargetScriptReference is ExternalReference)
                {
                    myExternalCount.Add(method.MethodName);
                } else if (method.TargetScriptReference is LocalReference localReference)
                {
                    var scriptElement = assetDocumentHierarchyElement.GetHierarchyElement(null, localReference.LocalDocumentAnchor, myUnityInterningCache, null);
                    if (scriptElement is IScriptComponentHierarchy script)
                    {
                        myLocalUsages.Add(method.MethodName, new AssetMethodData(LocalReference.Null,
                            method.MethodName, TextRange.InvalidRange,
                            method.Mode, method.Type, script.GetScriptReference(myUnityInterningCache)));
                    }
                    else
                    {
                        myExternalCount.Add(method.MethodName);
                    }
                }
            }
            
            myPsiSourceFileToMethods.Add(sourceFile, groupMethods);
        }

        public bool IsPossibleEventHandler(IDeclaredElement declaredElement)
        {
            myShellLocks.AssertReadAccessAllowed();
            
            if (declaredElement is IProperty property)
            {
                var setter = property.Setter;
                if (setter != null && myShortNameToScriptTarget.GetValues(setter.ShortName).Length > 0)
                    return true;
                    
                var getter = property.Getter;
                if (getter != null && myShortNameToScriptTarget.GetValues(getter.ShortName).Length > 0)
                    return true;

                return false;
            }
                
            return myShortNameToScriptTarget.GetValues(declaredElement.ShortName).Length > 0;
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
        
        public int GetAssetUsagesCountInner(IDeclaredElement declaredElement, out bool estimatedResult)
        {
            myShellLocks.AssertReadAccessAllowed();
            estimatedResult = false;
            if (!(declaredElement is IClrDeclaredElement clrDeclaredElement))
                return 0;

            if (!IsPossibleEventHandler(declaredElement))
                return 0;

            if (myExternalCount.GetCount(declaredElement.ShortName) > 0)
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
                var guid = (assetMethodData.TargetScriptReference as ExternalReference)?.ExternalAssetGuid;
                if (guid == null)
                    continue;
                
                var symbolTable = GetReferenceSymbolTable(solution, module, assetMethodData, guid);
                var resolveResult = symbolTable.GetResolveResult(assetMethodData.MethodName);
                if (resolveResult.ResolveErrorType == ResolveErrorType.OK && Equals(resolveResult.DeclaredElement, declaredElement))
                {
                    usageCount += c;
                }
            }

            return usageCount;
        }

        private string GetScriptGuid(AssetMethodData assetMethodData)
        {
            var reference = assetMethodData.TargetScriptReference;
            var scriptComponent = myAssetDocumentHierarchyElementContainer.GetHierarchyElement(reference, true) as IScriptComponentHierarchy;
            var guid = scriptComponent?.GetScriptReference(myUnityInterningCache)?.ExternalAssetGuid; 
            
            return guid;
        }

        public IEnumerable<AssetMethodData> GetAssetUsagesFor(IPsiSourceFile psiSourceFile, IDeclaredElement declaredElement)
        {
            myShellLocks.AssertReadAccessAllowed();
            
            var result = new List<AssetMethodData>();
            if (!myPsiSourceFileToMethods.TryGetValue(psiSourceFile, out var methods))
                return EmptyList<AssetMethodData>.Enumerable;
                
            var assetMethodData = methods.GetValuesSafe(declaredElement.ShortName);
            foreach (var methodData in assetMethodData)
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
        
        private ISymbolTable GetReferenceSymbolTable(ISolution solution, IPsiModule psiModule, AssetMethodData assetMethodData, string assetGuid)
        {
            var targetType = AssetUtils.GetTypeElementFromScriptAssetGuid(solution, assetGuid);
            if (targetType == null)
                return EmptySymbolTable.INSTANCE;

            var symbolTable = ResolveUtil.GetSymbolTableByTypeElement(targetType, SymbolTableMode.FULL, psiModule);

            return symbolTable.Filter(assetMethodData.MethodName, IsMethodFilter.INSTANCE, OverriddenFilter.INSTANCE, new ExactNameFilter(assetMethodData.MethodName),
                new StaticFilter(new NonStaticAccessContext(null)), new EventHandlerSymbolFilter(assetMethodData.Mode, assetMethodData.Type, targetType.Module));
        }

        public string Id => nameof(AssetMethodsElementContainer);
        public int Order => 0;
        public void Invalidate()
        {
            myShortNameToScriptTarget.Clear();
            myExternalCount.Clear();
            myPsiSourceFileToMethods.Clear();
            myLocalUsages.Clear();
        }

        public LocalList<IPsiSourceFile> GetPossibleFilesWithUsage(IDeclaredElement element)
        {
            if (element == null)
                return new LocalList<IPsiSourceFile>();

            var shortName = element.ShortName;

            var result = new LocalList<IPsiSourceFile>();
            foreach (var assetMethodData in myShortNameToScriptTarget.GetValues(shortName))
            {
                var location = assetMethodData.Location.OwnerId;
                var sourceFile = myPersistentIndexManager[location];
                if (sourceFile != null)
                    result.Add(sourceFile);
            }

            return result;
        }
    }
}