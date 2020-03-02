using System.Collections.Generic;
using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetUsages
{
    [SolutionComponent]
    public class AssetUsagesElementContainer : IUnityAssetDataElementContainer
    {
        private readonly DeferredCachesLocks myDeferredCachesLocks;
        private readonly MetaFileGuidCache myMetaFileGuidCache;

        public AssetUsagesElementContainer(DeferredCachesLocks deferredCachesLocks, MetaFileGuidCache metaFileGuidCache)
        {
            myDeferredCachesLocks = deferredCachesLocks;
            myMetaFileGuidCache = metaFileGuidCache;
        }
        
        private OneToCompactCountingSet<string, AssetUsage> myAssetUsages = new OneToCompactCountingSet<string, AssetUsage>();
        private Dictionary<IPsiSourceFile, OneToCompactCountingSet<string, AssetUsage>> myAssetUsagesPerFile = new Dictionary<IPsiSourceFile, OneToCompactCountingSet<string, AssetUsage>>();
        
        
        public IUnityAssetDataElement Build(Lifetime lifetime, IPsiSourceFile currentSourceFile, AssetDocument assetDocument)
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
            foreach (var assetUsage in dataElement.AssetUsages)
            {
                foreach (var dependency in assetUsage.Dependencies)
                {
                    if (dependency is ExternalReference externalReference)
                    {
                        myAssetUsages.Remove(externalReference.ExternalAssetGuid, assetUsage);

                        var set = myAssetUsagesPerFile[sourceFile];
                        set.Remove(externalReference.ExternalAssetGuid, assetUsage);
                        if (set.Count == 0)
                            myAssetUsagesPerFile.Remove(sourceFile);
                    }
                }
            }
        }

        public void Merge(IPsiSourceFile sourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElement unityAssetDataElement)
        {
            var dataElement = unityAssetDataElement as AssetUsagesDataElement;
            foreach (var assetUsage in dataElement.AssetUsages)
            {
                foreach (var dependency in assetUsage.Dependencies)
                {
                    if (dependency is ExternalReference externalReference)
                    {
                        myAssetUsages.Add(externalReference.ExternalAssetGuid, assetUsage);

                        if (!myAssetUsagesPerFile.TryGetValue(sourceFile, out var set))
                        {
                            set = new OneToCompactCountingSet<string, AssetUsage>();
                            myAssetUsagesPerFile[sourceFile] = set;
                        }

                        set.Add(externalReference.ExternalAssetGuid, assetUsage);
                    }
                }
            }
        }

        public int GetUsagesCount(IClassLikeDeclaration classLikeDeclaration, out bool estimatedResult)
        {
            // TODO : prefabs
            estimatedResult = false;
            
            return myDeferredCachesLocks.ExecuteUnderReadLock(_ =>
            {
                var sourceFile = classLikeDeclaration.GetSourceFile();
                if (sourceFile == null)
                    return 0;

                var declaredElement = classLikeDeclaration.DeclaredElement;
                if (declaredElement == null)
                    return 0;

                var guid = GetGuidFor(declaredElement);

                return myAssetUsages.GetOrEmpty(guid).Count;
            });
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
            return myDeferredCachesLocks.ExecuteUnderReadLock(_ =>
            {
                var guid = GetGuidFor(declaredElement);
                
                if (myAssetUsagesPerFile.TryGetValue(sourceFile, out var set))
                    return set.GetValues(guid).ToList();
                return Enumerable.Empty<AssetUsage>();
            });
        }

        private string GetGuidFor(ITypeElement typeElement)
        {
            var sourceFile = typeElement.GetDeclarations().FirstOrDefault()?.GetSourceFile();
            if (sourceFile == null)
                return null;
            
            if (typeElement.TypeParameters.Count != 0)
                return null;

            if (typeElement.GetContainingType() != null)
                return null;

            if (!typeElement.ShortName.Equals(sourceFile.GetLocation().NameWithoutExtension))
                return null;

            var guid = myMetaFileGuidCache.GetAssetGuid(sourceFile);
            return guid;
        }
    }
}