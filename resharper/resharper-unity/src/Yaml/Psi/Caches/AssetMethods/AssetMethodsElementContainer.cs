using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetMethods
{
    [SolutionComponent]
    public class AssetMethodsElementContainer : IUnityAssetDataElementContainer
    {
        private readonly ISolution mySolution;
        private readonly DeferredCachesLocks myDeferredCachesLocks;

        public AssetMethodsElementContainer(ISolution solution, DeferredCachesLocks deferredCachesLocks)
        {
            mySolution = solution;
            myDeferredCachesLocks = deferredCachesLocks;
        }
        
        private static readonly StringSearcher ourMethodNameSearcher = new StringSearcher("m_MethodName", false);
        private readonly OneToCompactCountingSet<string, AssetMethodData> myShortNameToScriptTarget = new OneToCompactCountingSet<string, AssetMethodData>();
        
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

                        var methodName = methodDescription.GetValue("m_MethodName").GetPlainScalarText();
                        if (methodName == null)
                            continue;
                        
                        var arguments = methodDescription.GetValue("m_Arguments") as IBlockMappingNode;
                        var modeText = methodDescription.GetValue("m_Mode").GetPlainScalarText();
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
                        
                        result.Add(new AssetMethodData(currentSourceFile.GetPersistentID(), methodName, argMode, type, fileID));                        
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
        }

        public void Merge(IPsiSourceFile sourceFile, IUnityAssetDataElement unityAssetDataElement)
        {
            var element = unityAssetDataElement as AssetMethodsDataElement;
            foreach (var method in element.Methods)
            {
                myShortNameToScriptTarget.Add(method.MethodName, method);
            }
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
                    // var scriptAnchorSourceFile = GetSourceFileWithPointedYamlDocument(sourceFile, scriptAnchor, myGuidCache);
                    var guid = "a1d497fe1c6a82f4d946c6867da502bd";//GetScriptGuid(scriptAnchorSourceFile, scriptAnchor.LocalDocumentAnchor);
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

        public string Id => nameof(AssetMethodsElementContainer);
    }
}