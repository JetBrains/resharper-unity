using System;
using System.Collections.Concurrent;
using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy
{
    [SolutionComponent]
    public class AssetDocumentHierarchyElementContainer : IUnityAssetDataElementContainer
    {
        private readonly DeferredCachesLocks myLocks;
        private readonly IPersistentIndexManager myManager;
        private readonly UnityExternalFilesPsiModule myPsiModule;
        private readonly MetaFileGuidCache myMetaFileGuidCache;

        private readonly ConcurrentDictionary<IPsiSourceFile, AssetDocumentHierarchyElement> myAssetDocumentsHierarchy =
            new ConcurrentDictionary<IPsiSourceFile, AssetDocumentHierarchyElement>();

        public AssetDocumentHierarchyElementContainer(DeferredCachesLocks locks, IPersistentIndexManager manager, UnityExternalFilesModuleFactory psiModuleProvider, MetaFileGuidCache metaFileGuidCache)
        {
            myLocks = locks;
            myManager = manager;
            myPsiModule = psiModuleProvider.PsiModule;
            myMetaFileGuidCache = metaFileGuidCache;
        }

        public IUnityAssetDataElement Build(Lifetime lifetime, IPsiSourceFile currentSourceFile, AssetDocument assetDocument)
        {
            if (AssetUtils.IsMonoBehaviourDocument(assetDocument.Buffer))
            {
                var entries = assetDocument.Document.FindRootBlockMapEntries()?.Entries;
                if (entries == null)
                    return null;

                var anchor = assetDocument.Document.GetLocalDocumentAnchor();
                AssetDocumentReference documentReference = null;

                foreach (var entry in entries)
                {
                    if (entry.Key.MatchesPlainScalarText(UnityYamlConstants.ScriptProperty))
                    {
                        documentReference = entry.Content.Value.AsFileID();
                        break;
                    }
                }

                if (documentReference != null && anchor != null)
                {
                    return new AssetDocumentHierarchyElement(
                            new ScriptComponentHierarchy(new LocalReference(currentSourceFile.PsiStorage.PersistentIndex, anchor),
                            new ExternalReference(documentReference.ExternalAssetGuid, documentReference.LocalDocumentAnchor)));
                }
            }
            return null;
        }

        public void Drop(IPsiSourceFile sourceFile, IUnityAssetDataElement unityAssetDataElement)
        {
            myAssetDocumentsHierarchy.TryRemove(sourceFile, out _);
        }

        public void Merge(IPsiSourceFile sourceFile, IUnityAssetDataElement unityAssetDataElement)
        {
            myAssetDocumentsHierarchy[sourceFile] = unityAssetDataElement as AssetDocumentHierarchyElement;
        }

        public IHierarchyElement GetHierarchyElement(IHierarchyReference reference)
        {
            return myLocks.ExecuteUnderReadLock(lf =>
            {
                var sourceFile = GetSourceFile(reference);
                if (sourceFile == null)
                    return null;

                return myAssetDocumentsHierarchy[sourceFile].GetHierarchyElement(reference.LocalDocumentAnchor);
            });
        }

        private IPsiSourceFile GetSourceFile(IHierarchyReference hierarchyReference)
        {
            switch (hierarchyReference)
            {
                case LocalReference localReference:
                    return myManager[localReference.OwnerId];
                case ExternalReference externalReference:
                    var paths = myMetaFileGuidCache.GetAssetFilePathsFromGuid(externalReference.ExternalAssetGuid);
                    if (paths.Count != 1)
                        return null;

                    return myPsiModule.TryGetFileByPath(paths[0], out var result) ? result : null;

                default:
                    throw new InvalidOperationException();
            }
        }
        
        
        public string Id => nameof(AssetDocumentHierarchyElementContainer);
    }
}