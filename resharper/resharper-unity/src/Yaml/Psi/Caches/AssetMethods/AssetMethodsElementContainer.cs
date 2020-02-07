using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Filters;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetMethods
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
        
        public IUnityAssetDataElement Build(Lifetime lifetime, IPsiSourceFile currentSourceFile, AssetDocument assetDocument)
        {
            var buffer = assetDocument.Buffer;
            if (ourMethodNameSearcher.Find(buffer) < 0)
                return null;

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
                        result.Add(new AssetMethodData(currentSourceFile.PsiStorage.PersistentIndex, methodName, range,
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
            }
            
            myPsiSourceFileToMethods.Add(sourceFile, groupMethods);
        }

        public bool IsEventHandler(IDeclaredElement declaredElement)
        {
            return myDeferredCachesLocks.ExecuteUnderReadLock(lifetime =>
            {
                var sourceFiles = declaredElement.GetSourceFiles();

                // The methods and property setters that we are interested in will only have a single source file
                if (sourceFiles.Count != 1)
                    return false;

                foreach (var assetMethodData in myShortNameToScriptTarget.GetValues(declaredElement.ShortName))
                {
                    var guid = GetScriptGuid(assetMethodData);
                    if (guid == null)
                        continue;
                
                    var invokedType = UnityObjectPsiUtil.GetTypeElementFromScriptAssetGuid(mySolution, guid);
                    if (invokedType != null)
                    {
                        var members = invokedType.GetAllClassMembers(declaredElement.ShortName);
                        foreach (var member in members)
                        {
                            if (Equals(member.Element, declaredElement))
                                return true;
                        }
                    }
                }

                return false;     
            });
        }

        private string GetScriptGuid(AssetMethodData assetMethodData)
        {
            var reference = assetMethodData.TargetScriptReference;
            var scriptComponent = myAssetDocumentHierarchyElementContainer.GetHierarchyElement(reference) as ScriptComponentHierarchy;
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
                    var symbolTable = GetReferenceSymbolTable(psiSourceFile, methodData);
                    if (symbolTable.GetResolveResult(methodData.MethodName).ResolveErrorType == ResolveErrorType.OK)
                    {
                        result.Add(methodData);
                    }
                }
                
                return result;
            });
        }
        
        private ISymbolTable GetReferenceSymbolTable(IPsiSourceFile owner, AssetMethodData assetMethodData)
        {
            var assetGuid = GetScriptGuid(assetMethodData);
            var targetType = UnityObjectPsiUtil.GetTypeElementFromScriptAssetGuid(owner.GetSolution(), assetGuid);
            if (targetType == null)
                return EmptySymbolTable.INSTANCE;

            var symbolTable = ResolveUtil.GetSymbolTableByTypeElement(targetType, SymbolTableMode.FULL, owner.GetPsiModule());

            return symbolTable.Filter(assetMethodData.MethodName, IsMethodFilter.INSTANCE, OverriddenFilter.INSTANCE, new ExactNameFilter(assetMethodData.MethodName),
                new StaticFilter(new NonStaticAccessContext(null)), new EventHandlerSymbolFilter(assetMethodData.Mode, assetMethodData.Type, targetType.Module));
        }

        public string Id => nameof(AssetMethodsElementContainer);
    }
}