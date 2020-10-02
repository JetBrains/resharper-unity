using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetUsages
{
    [SolutionComponent]
    public class AssetScriptUsagesElementContainer : IScriptUsagesElementContainer
    {
        private readonly IShellLocks myShellLocks;
        private readonly MetaFileGuidCache myMetaFileGuidCache;

        public AssetScriptUsagesElementContainer(IShellLocks shellLocks, MetaFileGuidCache metaFileGuidCache)
        {
            myShellLocks = shellLocks;
            myMetaFileGuidCache = metaFileGuidCache;
        }
        
        private readonly CountingSet<Guid> myUsagesCount = new CountingSet<Guid>();
        private readonly OneToCompactCountingSet<Guid, IPsiSourceFile> myUsageToSourceFiles = new OneToCompactCountingSet<Guid, IPsiSourceFile>();
        private readonly Dictionary<IPsiSourceFile, IUnityAssetDataElementPointer> myPointers = new Dictionary<IPsiSourceFile, IUnityAssetDataElementPointer>();

        public IUnityAssetDataElement CreateDataElement(IPsiSourceFile sourceFile)
        {
            return new AssetUsagesDataElement();
        }

        public object Build(SeldomInterruptChecker checker, IPsiSourceFile currentAssetSourceFile, AssetDocument assetDocument)
        {
            // TODO: deps for other assets
            if (AssetUtils.IsMonoBehaviourDocument(assetDocument.Buffer))
            {                
                var anchorRaw = AssetUtils.GetAnchorFromBuffer(assetDocument.Buffer);
                bool stripped = AssetUtils.IsStripped(assetDocument.Buffer);
                if (stripped) // we will handle it in prefabs
                    return null;
                
                if (!anchorRaw.HasValue)
                    return null;

                var anchor = anchorRaw.Value;
                
                var entries = assetDocument.Document.FindRootBlockMapEntries()?.Entries;
                if (entries == null)
                    return null;
                
                var result = new LocalList<AssetScriptUsages>();
                foreach (var entry in entries)
                {
                    if (!entry.Key.MatchesPlainScalarText("m_Script"))
                        continue;

                    var deps = entry.Content.Value.ToHierarchyReference(currentAssetSourceFile);
                    if (deps is ExternalReference externalReference)
                        result.Add(new AssetScriptUsages(new LocalReference(currentAssetSourceFile.Ptr().Id, anchor), externalReference));
                }

                return result;
            }

            return null;
        }

        public void Drop(IPsiSourceFile currentAssetSourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElement unityAssetDataElement)
        {
            var dataElement = unityAssetDataElement as AssetUsagesDataElement;
            foreach (var assetUsagePointer in dataElement.EnumerateAssetUsages())
            {
                if (!(assetUsagePointer is AssetScriptUsages assetScriptUsages)) continue;
                var guid = assetScriptUsages.UsageTarget.ExternalAssetGuid;
                myUsagesCount.Remove(guid);
                myUsageToSourceFiles.Remove(guid, currentAssetSourceFile);
            }

            myPointers.Remove(currentAssetSourceFile);
        }

        public void Merge(IPsiSourceFile currentAssetSourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElementPointer unityAssetDataElementPointer, IUnityAssetDataElement unityAssetDataElement)
        {
            myPointers[currentAssetSourceFile] = unityAssetDataElementPointer;
            var dataElement = unityAssetDataElement as AssetUsagesDataElement;
            foreach (var assetUsagePointer in dataElement.EnumerateAssetUsages())
            {
                if (!(assetUsagePointer is AssetScriptUsages assetScriptUsages)) continue;
                var guid = assetScriptUsages.UsageTarget.ExternalAssetGuid;
                myUsagesCount.Add(guid);
                myUsageToSourceFiles.Add(guid, currentAssetSourceFile);
            }
        }

        public int GetScriptUsagesCount(IClassLikeDeclaration classLikeDeclaration, out bool estimatedResult)
        {
            myShellLocks.AssertReadAccessAllowed();
            
            // TODO : prefabs
            estimatedResult = false;
            
            var sourceFile = classLikeDeclaration.GetSourceFile();
            if (sourceFile == null)
                return 0;

            var declaredElement = classLikeDeclaration.DeclaredElement;
            if (declaredElement == null)
                return 0;

            var guid = AssetUtils.GetGuidFor(myMetaFileGuidCache, declaredElement);
            if (guid == null)
                return 0;

            return myUsagesCount.GetCount(guid.Value);
        }
        
        public string Id => nameof(AssetScriptUsagesElementContainer);
        public int Order => 0;
        public void Invalidate()
        {
            myUsageToSourceFiles.Clear();
            myUsagesCount.Clear();
            myPointers.Clear();
        }

        public IEnumerable<IScriptUsage> GetScriptUsagesFor(IPsiSourceFile sourceFile, ITypeElement declaredElement)
        {
            myShellLocks.AssertReadAccessAllowed();
            var element = myPointers[sourceFile].GetElement(sourceFile, Id) as AssetUsagesDataElement;
            if (element == null) return Enumerable.Empty<IScriptUsage>();
            var guid = AssetUtils.GetGuidFor(myMetaFileGuidCache, declaredElement);
            if (guid == null) return Enumerable.Empty<IScriptUsage>();
            return element
                .EnumerateAssetUsages()
                .Cast<AssetScriptUsages>()
                .Where(t => t.UsageTarget.ExternalAssetGuid == guid)
                .Cast<IScriptUsage>();
        }

        public LocalList<IPsiSourceFile> GetPossibleFilesWithScriptUsages(IClass scriptClass)
        {
            var guid = AssetUtils.GetGuidFor(myMetaFileGuidCache, scriptClass);
            if (guid == null) 
                return new LocalList<IPsiSourceFile>();

            var result = new LocalList<IPsiSourceFile>();
            foreach (var assetUsage in myUsageToSourceFiles.GetValues(guid.Value))
            {
                result.Add(assetUsage);
            }

            return result;
        }
    }
}