using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetScriptUsages;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimatorUsages
{
    [SolutionComponent]
    public class AnimatorGameObjectUsagesContainer : IUnityAssetDataElementContainer
    {
        private readonly OneToCompactCountingSet<LocalReference, Guid> myGameObjectReferenceToControllerGuid = new();
        private readonly Dictionary<IPsiSourceFile, IUnityAssetDataElementPointer> myPointers = new();
        
        [NotNull] private readonly IShellLocks myShellLocks;
        private readonly AssetDocumentHierarchyElementContainer myAssetDocumentHierarchyElementContainer;
        public int Order => 0;

        public string Id => nameof(AnimatorGameObjectUsagesContainer);
        
        public AnimatorGameObjectUsagesContainer(
            [NotNull] IShellLocks locks,
            AssetDocumentHierarchyElementContainer assetDocumentHierarchyElementContainer)
        {
            myShellLocks = locks;
            myAssetDocumentHierarchyElementContainer = assetDocumentHierarchyElementContainer;
        }

        public IUnityAssetDataElement CreateDataElement(IPsiSourceFile sourceFile)
        {
            return new AnimatorGameObjectDataElement();
        }

        public bool IsApplicable(IPsiSourceFile currentAssetSourceFile)
        {
            return currentAssetSourceFile.IsScene() || currentAssetSourceFile.IsPrefab();
        }

        public object Build(IPsiSourceFile currentAssetSourceFile, AssetDocument assetDocument)
        {
            if (AssetUtils.IsAnimator(assetDocument.Buffer))
            {
                var anchorRaw = AssetUtils.GetAnchorFromBuffer(assetDocument.Buffer);
                var stripped = AssetUtils.IsStripped(assetDocument.Buffer);
                if (stripped) // we will handle it in prefabs
                    return null;

                if (!anchorRaw.HasValue)
                    return null;

                var anchor = anchorRaw.Value;

                var result = new LocalList<AssetScriptUsage>();
                var entry = assetDocument.Document.GetUnityObjectPropertyValue<INode>(UnityYamlConstants.ControllerProperty);
                var deps = entry.ToHierarchyReference(currentAssetSourceFile);
                if (deps is ExternalReference externalReference)
                    result.Add(new AssetScriptUsage(new LocalReference(currentAssetSourceFile.Ptr().Id, anchor), externalReference));

                return result;
            }

            return null;
        }

        public void Drop(IPsiSourceFile currentAssetSourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElement unityAssetDataElement)
        {
            var dataElement = (AnimatorGameObjectDataElement)unityAssetDataElement;
            foreach (var assetScriptUsages in dataElement.EnumerateAssetUsages())
            {
                myGameObjectReferenceToControllerGuid.Remove(assetScriptUsages.Location);
            }

            myPointers.Remove(currentAssetSourceFile);
        }

        public void Merge(IPsiSourceFile currentAssetSourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElementPointer unityAssetDataElementPointer, IUnityAssetDataElement unityAssetDataElement)
        {
            var dataElement = (AnimatorGameObjectDataElement)unityAssetDataElement;
            myPointers[currentAssetSourceFile] = unityAssetDataElementPointer;
            foreach (var assetUsagePointer in dataElement.EnumerateAssetUsages())
            {
                myGameObjectReferenceToControllerGuid.Add(assetUsagePointer.Location, assetUsagePointer.UsageTarget.ExternalAssetGuid);
            }
        }
        public void Invalidate()
        {
            myGameObjectReferenceToControllerGuid.Clear();
            myPointers.Clear();
        }

        public Guid[] GetAnimatorsFromGameObject(LocalReference gameObjectReference)
        {
            myShellLocks.AssertReadAccessAllowed();
            var result = new List<Guid>();
                
            foreach (var pair in myGameObjectReferenceToControllerGuid)
            {
                if (myAssetDocumentHierarchyElementContainer.GetHierarchyElement(pair.Key, true) is IComponentHierarchy he 
                    && he.OwningGameObject.Equals(gameObjectReference))
                    result.AddRange(pair.Value.Select(a=>a.Key).ToArray());
            }
            
            return result.ToArray();
        }
    }
}