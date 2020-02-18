using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetInspectorValues.Deserializers;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
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
        private readonly IEnumerable<IAssetInspectorValueDeserializer> myAssetInspectorValueDeserializers;

        private readonly ConcurrentDictionary<IPsiSourceFile, AssetDocumentHierarchyElement> myAssetDocumentsHierarchy =
            new ConcurrentDictionary<IPsiSourceFile, AssetDocumentHierarchyElement>();

        public AssetDocumentHierarchyElementContainer(DeferredCachesLocks locks, IPersistentIndexManager manager
            , UnityExternalFilesModuleFactory psiModuleProvider, MetaFileGuidCache metaFileGuidCache, IEnumerable<IAssetInspectorValueDeserializer> assetInspectorValueDeserializers)
        {
            myLocks = locks;
            myManager = manager;
            myPsiModule = psiModuleProvider.PsiModule;
            myMetaFileGuidCache = metaFileGuidCache;
            myAssetInspectorValueDeserializers = assetInspectorValueDeserializers;
        }

        public IUnityAssetDataElement Build(Lifetime lifetime, IPsiSourceFile currentSourceFile, AssetDocument assetDocument)
        {
            var anchor = AssetUtils.GetAnchorFromBuffer(assetDocument.Buffer);
            var isStripped = AssetUtils.IsStripped(assetDocument.Buffer);
            var gameObject = AssetUtils.GetGameObject(assetDocument.Buffer)?.ToReference(currentSourceFile);
            var prefabInstance = AssetUtils.GetPrefabInstance(assetDocument.Buffer)?.ToReference(currentSourceFile) as LocalReference;
            var correspondingSourceObject = AssetUtils.GetCorrespondingSourceObject(assetDocument.Buffer)?.ToReference(currentSourceFile) as ExternalReference;
            var location = new LocalReference(currentSourceFile.PsiStorage.PersistentIndex, anchor);

            if (anchor == null)
                return null;
            
            if (AssetUtils.IsMonoBehaviourDocument(assetDocument.Buffer))
            {
                var entries = assetDocument.Document.FindRootBlockMapEntries()?.Entries;
                if (entries == null)
                    return null;

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
                            new ScriptComponentHierarchy(location,
                            new ExternalReference(documentReference.ExternalAssetGuid, documentReference.LocalDocumentAnchor),
                            gameObject,
                            prefabInstance,
                            correspondingSourceObject,
                            isStripped
                            ));
                }
            } else if (AssetUtils.IsTransform(assetDocument.Buffer))
            {
                var father = AssetUtils.GetTransformFather(assetDocument.Buffer)?.ToReference(currentSourceFile);
                var rootIndex = AssetUtils.GetRootIndex(assetDocument.Buffer);
                return new AssetDocumentHierarchyElement(
                    new TransformHierarchy(location, gameObject, father, rootIndex, prefabInstance, correspondingSourceObject, isStripped));
            } else if (AssetUtils.IsGameObject(assetDocument.Buffer))
            {
                var name = AssetUtils.GetGameObjectName(assetDocument.Buffer);
                if (name == null)
                    return null;
                
                return new AssetDocumentHierarchyElement(
                    new GameObjectHierarchy(location, name, prefabInstance, correspondingSourceObject, isStripped));
            } else if (AssetUtils.IsPrefabModification(assetDocument.Buffer))
            {
                var modification = AssetUtils.GetPrefabModification(assetDocument.Document);
                var parentTransform = modification?.GetValue("m_TransformParent")?.AsFileID()?.ToReference(currentSourceFile);
                var modifications = modification?.GetValue("m_Modifications") as IBlockSequenceNode;
                var result = new List<PrefabModification>();
                if (modifications != null)
                {
                    foreach (var entry in modifications.Entries)
                    {
                        var map = entry.Value as IBlockMappingNode;
                        if (map == null)
                            continue;

                        var target = map.GetValue("target").AsFileID()?.ToReference(currentSourceFile);
                        if (target == null)
                            continue;
                        
                        var name = map.GetValue("propertyPath").GetPlainScalarText();
                        if (name == null)
                            continue;
                        
                        var valueNode = map.FindMapEntryBySimpleKey("value")?.Content;
                        if (valueNode == null)
                            continue;
                        
                        IAssetValue value = null;
                        foreach (var assetInspectorValueDeserializer in myAssetInspectorValueDeserializers)
                        {
                            if (assetInspectorValueDeserializer.TryGetInspectorValue(currentSourceFile, valueNode, out value))
                                break;
                        }
                        if (value == null)
                            continue;
                        
                        result.Add(new PrefabModification(target, name, value));
                    }
                }
                return new AssetDocumentHierarchyElement(new PrefabInstanceHierarchy(location, parentTransform, result));
            }
            else // regular component
            {
                var name = AssetUtils.GetRawComponentName(assetDocument.Buffer);
                if (name == null)
                    return null;

                return new AssetDocumentHierarchyElement(
                   new ComponentHierarchy(name, location, gameObject, prefabInstance, correspondingSourceObject, isStripped));
            }
            return null;
        }

        public void Drop(IPsiSourceFile sourceFile, IUnityAssetDataElement unityAssetDataElement)
        {
            myAssetDocumentsHierarchy.TryRemove(sourceFile, out _);
        }

        public void Merge(IPsiSourceFile sourceFile, IUnityAssetDataElement unityAssetDataElement)
        {
            var element = unityAssetDataElement as AssetDocumentHierarchyElement;
            myAssetDocumentsHierarchy[sourceFile] = element;
            element.RestoreHierarchy();
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
        public int Order => int.MaxValue;
    }
}