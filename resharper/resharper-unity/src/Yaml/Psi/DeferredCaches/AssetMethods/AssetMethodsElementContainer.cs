using System.Collections.Generic;
using System.Linq;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
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

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetMethods
{
    [SolutionComponent]
    public class AssetMethodsElementContainer : IUnityAssetDataElementContainer
    {
        private readonly ISolution mySolution;
        private readonly AssetDocumentHierarchyElementContainer myAssetDocumentHierarchyElementContainer;
        private readonly DeferredCachesLocks myDeferredCachesLocks;

        public AssetMethodsElementContainer(ISolution solution, AssetDocumentHierarchyElementContainer assetDocumentHierarchyElementContainer, DeferredCachesLocks deferredCachesLocks)
        {
            mySolution = solution;
            myAssetDocumentHierarchyElementContainer = assetDocumentHierarchyElementContainer;
            myDeferredCachesLocks = deferredCachesLocks;
        }
        
        private static readonly StringSearcher ourMethodNameSearcher = new StringSearcher("m_MethodName", false);
        private readonly OneToCompactCountingSet<string, AssetMethodData> myShortNameToScriptTarget = new OneToCompactCountingSet<string, AssetMethodData>();
        private readonly Dictionary<IPsiSourceFile, OneToListMap<string, AssetMethodData>> myPsiSourceFileToMethods = new Dictionary<IPsiSourceFile, OneToListMap<string, AssetMethodData>>();
        
        private readonly CountingSet<string> myExternalCount = new CountingSet<string>();
        private readonly OneToCompactCountingSet<string, AssetMethodData> myLocalUsages = new OneToCompactCountingSet<string, AssetMethodData>();
        
        public IUnityAssetDataElement Build(Lifetime lifetime, IPsiSourceFile currentSourceFile, AssetDocument assetDocument)
        {
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

            var result = new List<AssetMethodData>();
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
                        result.Add(new AssetMethodData(new LocalReference(currentSourceFile.PsiStorage.PersistentIndex, anchor), methodName, range,
                            argMode, type, fileID.ToReference(currentSourceFile)));                        
                    }
                }
            }

            if (result.Count > 0)
                return new AssetMethodsDataElement(result);
            return null;
        }

        public void Drop(IPsiSourceFile sourceFile, IUnityAssetDataElement unityAssetDataElement)
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
                    if (myAssetDocumentHierarchyElementContainer.GetHierarchyElement(localReference, false) is IScriptComponentHierarchy script)
                    {
                        myLocalUsages.Remove(method.MethodName, new AssetMethodData(LocalReference.Null, method.MethodName, TextRange.InvalidRange,
                            method.Mode, method.Type, script.ScriptReference));
                    }
                }
            }

            myPsiSourceFileToMethods.Remove(sourceFile);
        }

        public void Merge(IPsiSourceFile sourceFile, IUnityAssetDataElement unityAssetDataElement)
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
                    if (myAssetDocumentHierarchyElementContainer.GetHierarchyElement(localReference, false) is IScriptComponentHierarchy script)
                    {
                        myLocalUsages.Add(method.MethodName, new AssetMethodData(LocalReference.Null, method.MethodName, TextRange.InvalidRange,
                            method.Mode, method.Type, script.ScriptReference));
                    }
                }
            }
            
            myPsiSourceFileToMethods.Add(sourceFile, groupMethods);
        }

        public bool IsPossibleEventHandler(IDeclaredElement declaredElement)
        {
            return myDeferredCachesLocks.ExecuteUnderReadLock(_ =>
            {
                return myShortNameToScriptTarget.GetValues(declaredElement.ShortName).Length > 0;
            });
        }
        
        public int GetAssetUsagesCount(IDeclaredElement declaredElement, out bool estimatedResult)
        {
            estimatedResult = false;
            if (!(declaredElement is IClrDeclaredElement clrDeclaredElement))
                return 0;
            
            var (count, estimated) = myDeferredCachesLocks.ExecuteUnderReadLock(_ =>
            {
                bool isEstimated = false;
                if (!IsPossibleEventHandler(declaredElement))
                    return (0, false);

                if (myExternalCount.GetCount(declaredElement.ShortName) > 0)
                    isEstimated = true;

                const int maxProcessCount = 5;
                if (myLocalUsages.GetOrEmpty(declaredElement.ShortName).Count > maxProcessCount)
                    isEstimated = true;

                var usageCount = 0;
                foreach (var (assetMethodData, c) in myLocalUsages.GetOrEmpty(declaredElement.ShortName).Take(maxProcessCount))
                {
                    var solution = declaredElement.GetSolution();
                    var module = clrDeclaredElement.Module;
                    
                    // we have already cache guid in merge method for methodData in myLocalUsages
                    var guid = (assetMethodData.TargetScriptReference as ExternalReference).NotNull("Expected External Reference").ExternalAssetGuid;
                    var symbolTable = GetReferenceSymbolTable(solution, module, assetMethodData, guid);
                    if (symbolTable.GetResolveResult(assetMethodData.MethodName).ResolveErrorType == ResolveErrorType.OK)
                    {
                        usageCount += c;
                    }
                }
                    
                return (usageCount, isEstimated);
            });

            estimatedResult = estimated;
            return count;
        }

        private string GetScriptGuid(AssetMethodData assetMethodData)
        {
            var reference = assetMethodData.TargetScriptReference;
            var scriptComponent = myAssetDocumentHierarchyElementContainer.GetHierarchyElement(reference, false) as IScriptComponentHierarchy;
            var guid = scriptComponent?.ScriptReference.ExternalAssetGuid; 
            
            return guid;
        }

        public IEnumerable<AssetMethodData> GetAssetUsagesFor(IPsiSourceFile psiSourceFile, IDeclaredElement declaredElement)
        {
            return myDeferredCachesLocks.ExecuteUnderReadLock(lf =>
            {
                var result = new List<AssetMethodData>();
                if (!myPsiSourceFileToMethods.TryGetValue(psiSourceFile, out var methods))
                    return EmptyList<AssetMethodData>.Enumerable;
                
                var assetMethodData = methods.GetValuesSafe(declaredElement.ShortName);
                foreach (var methodData in assetMethodData)
                {
                    var symbolTable = GetReferenceSymbolTable(psiSourceFile.GetSolution(), psiSourceFile.GetPsiModule(), methodData, GetScriptGuid(methodData));
                    if (symbolTable.GetResolveResult(methodData.MethodName).ResolveErrorType == ResolveErrorType.OK)
                    {
                        result.Add(methodData);
                    }
                }
                
                return result;
            });
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
    }
}