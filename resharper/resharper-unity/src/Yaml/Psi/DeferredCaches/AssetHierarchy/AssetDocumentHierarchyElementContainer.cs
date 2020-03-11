using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Deserializers;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy
{
    [SolutionComponent]
    public class AssetDocumentHierarchyElementContainer : IUnityAssetDataElementContainer
    {
        private readonly IPersistentIndexManager myManager;
        private readonly PrefabImportCache myPrefabImportCache;
        private readonly IShellLocks myShellLocks;
        private readonly UnityExternalFilesPsiModule myPsiModule;
        private readonly MetaFileGuidCache myMetaFileGuidCache;
        private readonly IEnumerable<IAssetInspectorValueDeserializer> myAssetInspectorValueDeserializers;

        private readonly ConcurrentDictionary<IPsiSourceFile, AssetDocumentHierarchyElement> myAssetDocumentsHierarchy =
            new ConcurrentDictionary<IPsiSourceFile, AssetDocumentHierarchyElement>();

        public AssetDocumentHierarchyElementContainer(IPersistentIndexManager manager, PrefabImportCache prefabImportCache, IShellLocks shellLocks,
            UnityExternalFilesModuleFactory psiModuleProvider, MetaFileGuidCache metaFileGuidCache, IEnumerable<IAssetInspectorValueDeserializer> assetInspectorValueDeserializers)
        {
            myManager = manager;
            myPrefabImportCache = prefabImportCache;
            myShellLocks = shellLocks;
            myPsiModule = psiModuleProvider.PsiModule;
            myMetaFileGuidCache = metaFileGuidCache;
            myAssetInspectorValueDeserializers = assetInspectorValueDeserializers;
        }

        public IUnityAssetDataElement Build(SeldomInterruptChecker checker, IPsiSourceFile currentSourceFile, AssetDocument assetDocument)
        {
            var anchorRaw = AssetUtils.GetAnchorFromBuffer(assetDocument.Buffer);
            if (!anchorRaw.HasValue)
                return null;

            var anchor = anchorRaw.Value;
            
            var isStripped = AssetUtils.IsStripped(assetDocument.Buffer);
            var gameObject = AssetUtils.GetGameObject(assetDocument.Buffer)?.ToReference(currentSourceFile) as LocalReference;
            var prefabInstance = AssetUtils.GetPrefabInstance(assetDocument.Buffer)?.ToReference(currentSourceFile) as LocalReference;
            var correspondingSourceObject = AssetUtils.GetCorrespondingSourceObject(assetDocument.Buffer)?.ToReference(currentSourceFile) as ExternalReference;
            var location = new LocalReference(currentSourceFile.PsiStorage.PersistentIndex, anchor);

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

                if (isStripped || documentReference != null)
                {
                    var scriptAnchorRaw = documentReference?.AnchorLong;
                    
                    return new AssetDocumentHierarchyElement(
                            new ScriptComponentHierarchy(location,
                            !scriptAnchorRaw.HasValue ? null : new ExternalReference(documentReference.ExternalAssetGuid, scriptAnchorRaw.Value),
                            gameObject,
                            prefabInstance,
                            correspondingSourceObject,
                            isStripped
                            ));
                }
            } else if (AssetUtils.IsTransform(assetDocument.Buffer))
            {
                var father = AssetUtils.GetTransformFather(assetDocument.Buffer)?.ToReference(currentSourceFile) as LocalReference;
                var rootIndex = AssetUtils.GetRootIndex(assetDocument.Buffer);
                return new AssetDocumentHierarchyElement(
                    new TransformHierarchy(location, gameObject, father, rootIndex, prefabInstance, correspondingSourceObject, isStripped));
            } else if (AssetUtils.IsGameObject(assetDocument.Buffer))
            {
                var name = AssetUtils.GetGameObjectName(assetDocument.Buffer);
                if (isStripped || name != null)
                {
                    return new AssetDocumentHierarchyElement(new GameObjectHierarchy(location, name, prefabInstance, correspondingSourceObject, isStripped));
                }
            } else if (AssetUtils.IsPrefabModification(assetDocument.Buffer))
            {
                var modification = AssetUtils.GetPrefabModification(assetDocument.Document);
                var parentTransform = modification?.GetValue("m_TransformParent")?.AsFileID()?.ToReference(currentSourceFile) as LocalReference;
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

                var sourcePrefabGuid = AssetUtils.GetSourcePrefab(assetDocument.Buffer)?.ToReference(currentSourceFile) as ExternalReference;
                if (sourcePrefabGuid == null)
                    return null;
                return new AssetDocumentHierarchyElement(new PrefabInstanceHierarchy(location, sourcePrefabGuid.ExternalAssetGuid, parentTransform, result));
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

        public void Drop(IPsiSourceFile sourceFile, AssetDocumentHierarchyElement hierarchyElement, IUnityAssetDataElement unityAssetDataElement)
        {
            myPrefabImportCache.Remove(sourceFile, unityAssetDataElement as AssetDocumentHierarchyElement);
            myAssetDocumentsHierarchy.TryRemove(sourceFile, out _);
        }

        public void Merge(IPsiSourceFile sourceFile, AssetDocumentHierarchyElement hierarchyElement, IUnityAssetDataElement unityAssetDataElement)
        {
            var element = unityAssetDataElement as AssetDocumentHierarchyElement;
            element.AssetDocumentHierarchyElementContainer = this;
            element.IsScene = sourceFile.GetLocation().ExtensionWithDot.Equals(UnityYamlConstants.Scene);
            myAssetDocumentsHierarchy[sourceFile] = element;
            element.RestoreHierarchy();

            myPrefabImportCache.Add(sourceFile, element);
        }

        public IHierarchyElement GetHierarchyElement(IHierarchyReference reference, bool prefabImport)
        {
            myShellLocks.AssertReadAccessAllowed();
            
            var sourceFile = GetSourceFile(reference, out var guid);
            if (sourceFile == null || guid == null)
                return null;
                
            return myAssetDocumentsHierarchy[sourceFile].GetHierarchyElement(guid, reference.LocalDocumentAnchor, prefabImport ? myPrefabImportCache : null);
        }

        public AssetDocumentHierarchyElement GetAssetHierarchyFor(IPsiSourceFile sourceFile)
        {
            myShellLocks.AssertReadAccessAllowed();
            
            if (myAssetDocumentsHierarchy.TryGetValue(sourceFile, out var result))
                return result;
            
            return null;
        }
        
        public AssetDocumentHierarchyElement GetAssetHierarchyFor(LocalReference location, out string guid)
        {
            myShellLocks.AssertReadAccessAllowed();
            
            var sourceFile = GetSourceFile(location, out guid);
            if (sourceFile == null || guid == null)
                return null;

            return myAssetDocumentsHierarchy.GetValueSafe(sourceFile);
        }
        
        private IPsiSourceFile GetSourceFile(IHierarchyReference hierarchyReference, out string guid)
        {
            switch (hierarchyReference)
            {
                case LocalReference localReference:
                    var sourceFile = myManager[localReference.OwnerId];
                    guid = sourceFile != null ? myMetaFileGuidCache.GetAssetGuid(sourceFile) : null;
                    return sourceFile;
                case ExternalReference externalReference:
                    guid = externalReference.ExternalAssetGuid;
                    var paths = myMetaFileGuidCache.GetAssetFilePathsFromGuid(guid);
                    if (paths.Count != 1)
                        return null;

                    return myPsiModule.TryGetFileByPath(paths[0], out var result) ? result : null;

                default:
                    throw new InvalidOperationException();
            }
        }
        
        
        public string Id => nameof(AssetDocumentHierarchyElementContainer);
        public int Order => int.MaxValue;
        public void Invalidate()
        {
            myAssetDocumentsHierarchy.Clear();
        }
    }
}