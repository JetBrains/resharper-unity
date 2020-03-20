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
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetUsages
{
    [SolutionComponent]
    public class AssetUsagesElementContainer : IUnityAssetDataElementContainer
    {
        private readonly IShellLocks myShellLocks;
        private readonly IPersistentIndexManager myPersistentIndexManager;
        private readonly MetaFileGuidCache myMetaFileGuidCache;

        public AssetUsagesElementContainer(IShellLocks shellLocks, IPersistentIndexManager persistentIndexManager, MetaFileGuidCache metaFileGuidCache)
        {
            myShellLocks = shellLocks;
            myPersistentIndexManager = persistentIndexManager;
            myMetaFileGuidCache = metaFileGuidCache;
        }
        
        private OneToCompactCountingSet<int, AssetUsagePointer> myAssetUsages = new OneToCompactCountingSet<int, AssetUsagePointer>();
        private Dictionary<IPsiSourceFile, OneToCompactCountingSet<int, AssetUsagePointer>> myAssetUsagesPerFile = new Dictionary<IPsiSourceFile, OneToCompactCountingSet<int, AssetUsagePointer>>();
        private Dictionary<IPsiSourceFile, IUnityAssetDataElementPointer> myPointers = new Dictionary<IPsiSourceFile, IUnityAssetDataElementPointer>();
        
        public IUnityAssetDataElement Build(SeldomInterruptChecker checker, IPsiSourceFile currentSourceFile, AssetDocument assetDocument)
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
                foreach (var entry in entries)
                {
                    if (!entry.Key.MatchesPlainScalarText("m_Script"))
                        continue;

                    var deps = entry.Content.Value.AsFileID()?.ToReference(currentSourceFile);
                    if (deps == null)
                        continue;
                    
                    return new AssetUsagesDataElement(
                        new AssetUsage(
                            new LocalReference(currentSourceFile.PsiStorage.PersistentIndex, anchor), new [] {deps}));
                }
            }

            return null;
        }

        public void Drop(IPsiSourceFile sourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElement unityAssetDataElement)
        {
            var dataElement = unityAssetDataElement as AssetUsagesDataElement;
            foreach (var assetUsagePointer in dataElement.EnumerateAssetUsages())
            {
                var assetUsage = dataElement.GetAssetUsage(assetUsagePointer);                
                foreach (var dependency in assetUsage.Dependencies)
                {
                    if (dependency is ExternalReference externalReference)
                    {
                        myAssetUsages.Remove(externalReference.ExternalAssetGuid.GetPlatformIndependentHashCode(), assetUsagePointer);

                        var set = myAssetUsagesPerFile[sourceFile];
                        set.Remove(externalReference.ExternalAssetGuid.GetPlatformIndependentHashCode(), assetUsagePointer);
                        if (set.Count == 0)
                            myAssetUsagesPerFile.Remove(sourceFile);
                    }
                }
            }

            myPointers.Remove(sourceFile);
        }

        public void Merge(IPsiSourceFile sourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElementPointer unityAssetDataElementPointer, IUnityAssetDataElement unityAssetDataElement)
        {
            myPointers[sourceFile] = unityAssetDataElementPointer;
            var dataElement = unityAssetDataElement as AssetUsagesDataElement;
            foreach (var assetUsagePointer in dataElement.EnumerateAssetUsages())
            {
                var assetUsage = dataElement.GetAssetUsage(assetUsagePointer);    
                foreach (var dependency in assetUsage.Dependencies)
                {
                    if (dependency is ExternalReference externalReference)
                    {
                        myAssetUsages.Add(externalReference.ExternalAssetGuid.GetPlatformIndependentHashCode(), assetUsagePointer);

                        if (!myAssetUsagesPerFile.TryGetValue(sourceFile, out var set))
                        {
                            set = new OneToCompactCountingSet<int, AssetUsagePointer>();
                            myAssetUsagesPerFile[sourceFile] = set;
                        }

                        set.Add(externalReference.ExternalAssetGuid.GetPlatformIndependentHashCode(), assetUsagePointer);
                    }
                }
            }
        }

        public int GetUsagesCount(IClassLikeDeclaration classLikeDeclaration, out bool estimatedResult)
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

            return myAssetUsages.GetOrEmpty(guid.GetPlatformIndependentHashCode()).Count;
        }
        
        public string Id => nameof(AssetUsagesElementContainer);
        public int Order => 0;
        public void Invalidate()
        {
            myAssetUsages.Clear();
            myAssetUsagesPerFile.Clear();
        }

        public IEnumerable<AssetUsage> GetAssetUsagesFor(IPsiSourceFile sourceFile, ITypeElement declaredElement)
        {
            myShellLocks.AssertReadAccessAllowed();

            var element = myPointers[sourceFile].Element as AssetUsagesDataElement;
            if (element == null)
                return Enumerable.Empty<AssetUsage>();
            
            var guid = AssetUtils.GetGuidFor(myMetaFileGuidCache, declaredElement);
                
            if (myAssetUsagesPerFile.TryGetValue(sourceFile, out var set))
                return set.GetValues(guid.GetPlatformIndependentHashCode()).Select(t => element.GetAssetUsage(t)).ToList();
            return Enumerable.Empty<AssetUsage>();
        }

        public LocalList<IPsiSourceFile>  GetPossibleFilesWithUsage(ITypeElement declaredElement)
        {
            var guid =AssetUtils.GetGuidFor(myMetaFileGuidCache, declaredElement);
            if (guid == null) 
                return new LocalList<IPsiSourceFile>();

            var result = new LocalList<IPsiSourceFile>();
            foreach (var assetUsage in myAssetUsages.GetValues(guid))
            {
                var location = assetUsage.Location.OwnerId;
                var sourceFile = myPersistentIndexManager[location];
                if (sourceFile != null)
                    result.Add(sourceFile);
            }

            return result;
        }
    }
}