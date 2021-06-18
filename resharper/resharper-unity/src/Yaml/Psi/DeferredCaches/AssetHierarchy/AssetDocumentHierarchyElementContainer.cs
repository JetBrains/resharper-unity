using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Stripped;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Deserializers;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy
{
    [SolutionComponent]
    public class AssetDocumentHierarchyElementContainer : IUnityAssetDataElementContainer
    {
        private readonly IPersistentIndexManager myManager;
        private readonly UnityExternalFilesPsiModule myPsiModule;
        private readonly MetaFileGuidCache myMetaFileGuidCache;

        private readonly PrefabImportCache myPrefabImportCache;
        private readonly IShellLocks myShellLocks;
        private readonly IEnumerable<IAssetInspectorValueDeserializer> myAssetInspectorValueDeserializers;

        private readonly ConcurrentDictionary<IPsiSourceFile, IUnityAssetDataElementPointer> myAssetDocumentsHierarchy =
            new ConcurrentDictionary<IPsiSourceFile, IUnityAssetDataElementPointer>();

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

        public IUnityAssetDataElement CreateDataElement(IPsiSourceFile sourceFile)
        {
            return new AssetDocumentHierarchyElement();
        }

        public bool IsApplicable(IPsiSourceFile currentAssetSourceFile)
        {
            return true;
        }

        public object Build(SeldomInterruptChecker checker, IPsiSourceFile currentAssetSourceFile, AssetDocument assetDocument)
        {
            var anchorRaw = AssetUtils.GetAnchorFromBuffer(assetDocument.Buffer);
            if (!anchorRaw.HasValue)
                return null;

            var anchor = anchorRaw.Value;
            var isStripped = AssetUtils.IsStripped(assetDocument.Buffer);
            var location = new LocalReference(currentAssetSourceFile.PsiStorage.PersistentIndex.NotNull("owningPsiPersistentIndex != null"), anchor);

            if (isStripped)
            {
                var prefabInstance = AssetUtils.GetPrefabInstance(currentAssetSourceFile, assetDocument.Buffer) as LocalReference?;
                var correspondingSourceObject = AssetUtils.GetCorrespondingSourceObject(currentAssetSourceFile, assetDocument.Buffer) as ExternalReference?;

                if (prefabInstance != null && correspondingSourceObject != null)
                    return new StrippedHierarchyElement(location, prefabInstance.Value, correspondingSourceObject.Value);

                return null;
            }

            var gameObject = AssetUtils.GetGameObjectReference(currentAssetSourceFile, assetDocument.Buffer) as LocalReference?;

            if (gameObject != null && AssetUtils.IsMonoBehaviourDocument(assetDocument.Buffer))
            {
                var documentReference =
                    assetDocument.Document.GetUnityObjectPropertyValue<INode>(UnityYamlConstants.ScriptProperty)
                        ?.ToHierarchyReference(currentAssetSourceFile);

                if (documentReference is ExternalReference scriptReference)
                {
                    return new ScriptComponentHierarchy(location, gameObject.Value, scriptReference);
                }
            } else if (gameObject != null && AssetUtils.IsTransform(assetDocument.Buffer))
            {
                var father = AssetUtils.GetTransformFather(currentAssetSourceFile, assetDocument.Buffer) as LocalReference?;
                if (father == null)
                    return null;

                var rootIndex = AssetUtils.GetRootIndex(assetDocument.Buffer);
                return new TransformHierarchy(location, gameObject.Value, father.Value, rootIndex);
            } else if (AssetUtils.IsGameObject(assetDocument.Buffer))
            {
                var name = AssetUtils.GetGameObjectName(assetDocument.Buffer);
                if (name != null)
                {
                    return new GameObjectHierarchy(location, name);
                }
            } else if (AssetUtils.IsPrefabModification(assetDocument.Buffer))
            {
                var modification = AssetUtils.GetPrefabModification(assetDocument.Document);
                var parentTransform =
                    modification?.GetMapEntryValue<INode>("m_TransformParent")
                        ?.ToHierarchyReference(currentAssetSourceFile) as LocalReference? ?? LocalReference.Null;
                var modifications = modification?.GetMapEntryValue<IBlockSequenceNode>("m_Modifications");
                var result = new List<PrefabModification>();
                if (modifications != null)
                {
                    foreach (var entry in modifications.Entries)
                    {
                        var map = entry.Value as IBlockMappingNode;

                        var target = map?.GetMapEntryValue<INode>("target")
                            .ToHierarchyReference(currentAssetSourceFile);
                        if (target == null)
                            continue;

                        var name = map.GetMapEntryPlainScalarText("propertyPath");
                        if (name == null)
                            continue;

                        var valueNode = map.GetMapEntry("value")?.Content;
                        if (valueNode == null)
                            continue;

                        IAssetValue value = null;
                        foreach (var assetInspectorValueDeserializer in myAssetInspectorValueDeserializers)
                        {
                            if (assetInspectorValueDeserializer.TryGetInspectorValue(currentAssetSourceFile, valueNode, out value))
                                break;
                        }

                        var objectReference = map.GetMapEntryValue<INode>("objectReference").ToHierarchyReference(currentAssetSourceFile);

                        var valueRange = valueNode.GetTreeTextRange();

                        result.Add(new PrefabModification(target, name, value,
                            new TextRange(assetDocument.StartOffset + valueRange.StartOffset.Offset,
                                assetDocument.StartOffset + valueRange.EndOffset.Offset),  objectReference));
                    }
                }

                var sourcePrefabGuid = AssetUtils.GetSourcePrefab(currentAssetSourceFile, assetDocument.Buffer) as ExternalReference?;
                if (sourcePrefabGuid == null)
                    return null;

                return new PrefabInstanceHierarchy(location, parentTransform, result, sourcePrefabGuid.Value.ExternalAssetGuid);
            }
            else if (gameObject != null)// regular component
            {
                var name = AssetUtils.GetRawComponentName(assetDocument.Buffer);
                if (name == null)
                    return null;

                return new ComponentHierarchy(location, gameObject.Value, name);
            }
            return null;
        }

        public void Drop(IPsiSourceFile currentAssetSourceFile, AssetDocumentHierarchyElement hierarchyElement, IUnityAssetDataElement unityAssetDataElement)
        {
            myPrefabImportCache.OnHierarchyRemoved(currentAssetSourceFile, unityAssetDataElement as AssetDocumentHierarchyElement);
            myAssetDocumentsHierarchy.TryRemove(currentAssetSourceFile, out _);
        }

        public void Merge(IPsiSourceFile currentAssetSourceFile, AssetDocumentHierarchyElement hierarchyElement, IUnityAssetDataElementPointer unityAssetDataElementPointer, IUnityAssetDataElement unityAssetDataElement)
        {
            var element = unityAssetDataElement as AssetDocumentHierarchyElement;
            myAssetDocumentsHierarchy[currentAssetSourceFile] = unityAssetDataElementPointer;
            element.RestoreHierarchy(this, currentAssetSourceFile);

            myPrefabImportCache.OnHierarchyCreated(currentAssetSourceFile, element);
        }

        public IHierarchyElement GetHierarchyElement(IHierarchyReference reference, bool prefabImport)
        {
            myShellLocks.AssertReadAccessAllowed();
            if (reference == null)
                return null;

            var sourceFile = GetSourceFile(reference, out var guid);
            if (sourceFile == null || guid == null)
                return null;

            var element = myAssetDocumentsHierarchy[sourceFile].GetElement(sourceFile, Id) as AssetDocumentHierarchyElement;
            if (element == null)
                return null;

            return element.GetHierarchyElement(guid.Value, reference.LocalDocumentAnchor, prefabImport ? myPrefabImportCache : null);
        }

        public AssetDocumentHierarchyElement GetAssetHierarchyFor(IPsiSourceFile sourceFile)
        {
            myShellLocks.AssertReadAccessAllowed();

            if (myAssetDocumentsHierarchy.TryGetValue(sourceFile, out var result))
                return result.GetElement(sourceFile, Id) as AssetDocumentHierarchyElement;

            return null;
        }

        public AssetDocumentHierarchyElement GetAssetHierarchyFor(LocalReference location, out Guid? guid)
        {
            myShellLocks.AssertReadAccessAllowed();

            var sourceFile = GetSourceFile(location, out guid);
            if (sourceFile == null || guid == null)
                return null;

            return myAssetDocumentsHierarchy.GetValueSafe(sourceFile)?.GetElement(sourceFile, Id) as AssetDocumentHierarchyElement;
        }

        public IPsiSourceFile GetSourceFile(IHierarchyReference hierarchyReference, out Guid? guid)
        {
            guid = null;
            if (hierarchyReference == null)
                return null;

            myShellLocks.AssertReadAccessAllowed();
            switch (hierarchyReference)
            {
                case LocalReference localReference:
                    var sourceFile = myManager[localReference.OwningPsiPersistentIndex];
                    guid = sourceFile != null ? myMetaFileGuidCache.GetAssetGuid(sourceFile) : null;
                    return sourceFile;
                case ExternalReference externalReference:
                    guid = externalReference.ExternalAssetGuid;
                    var paths = myMetaFileGuidCache.GetAssetFilePathsFromGuid(guid.Value);
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