using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.Utils;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetUsages
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
        public IUnityAssetDataElement Build(Lifetime lifetime, IPsiSourceFile currentSourceFile, AssetDocument assetDocument)
        {
            // TODO: deps for other assets
            if (AssetUtils.IsMonoBehaviourDocument(assetDocument.Buffer))
            {                
                var anchor = UnitySceneDataUtil.GetAnchorFromBuffer(assetDocument.Buffer);
                if (anchor == null)
                    return null;
                
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

        public void Drop(IPsiSourceFile sourceFile, IUnityAssetDataElement unityAssetDataElement)
        {
            var dataElement = unityAssetDataElement as AssetUsagesDataElement;
            foreach (var assetUsage in dataElement.AssetUsages)
            {
                foreach (var dependency in assetUsage.Dependencies)
                {
                    if (dependency is ExternalReference externalReference)
                        myAssetUsages.Remove(externalReference.ExternalAssetGuid, assetUsage);
                }
            }
        }

        public void Merge(IPsiSourceFile sourceFile, IUnityAssetDataElement unityAssetDataElement)
        {
            var dataElement = unityAssetDataElement as AssetUsagesDataElement;
            foreach (var assetUsage in dataElement.AssetUsages)
            {
                foreach (var dependency in assetUsage.Dependencies)
                {
                    if (dependency is ExternalReference externalReference)
                        myAssetUsages.Add(externalReference.ExternalAssetGuid, assetUsage);
                }
            }
        }

        public int GetUsagesCount(IClassLikeDeclaration classLikeDeclaration)
        {
            return myDeferredCachesLocks.ExecuteUnderReadLock(_ =>
            {
                var sourceFile = classLikeDeclaration.GetSourceFile();
                if (sourceFile == null)
                    return 0;
            
                if (classLikeDeclaration.TypeParameters.Count != 0)
                    return 0;

                if (!classLikeDeclaration.NameIdentifier.Name.Equals(sourceFile.Name))
                    return 0;

                var guid = myMetaFileGuidCache.GetAssetGuid(sourceFile);
                if (guid == null)
                    return 0;

                return myAssetUsages.GetOrEmpty(guid).Count;
            });
        }
        
        public string Id => nameof(AssetUsagesElementContainer);
    }
}