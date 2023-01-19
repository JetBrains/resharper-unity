using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetScriptUsages;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimatorUsages
{
    [SolutionComponent]
    public partial class AnimatorGameObjectUsagesContainer : IUnityAssetDataElementContainer
    {
        private readonly Dictionary<LocalReference, Guid> myGameObjectReferenceToControllerGuid = new();
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
            else if (AssetUtils.IsPrefabModification(assetDocument.Buffer))
                return ProcessPrefabModifications(currentAssetSourceFile, assetDocument);
            
            return null;
        }

        public void Drop(IPsiSourceFile currentAssetSourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElement unityAssetDataElement)
        {
            var dataElement = (AnimatorGameObjectDataElement)unityAssetDataElement;
            foreach (var assetScriptUsages in dataElement.EnumerateAssetUsages())
            {
                myGameObjectReferenceToControllerGuid.Remove(assetScriptUsages.Location);
            }
        }

        public void Merge(IPsiSourceFile currentAssetSourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElementPointer unityAssetDataElementPointer, IUnityAssetDataElement unityAssetDataElement)
        {
            var dataElement = (AnimatorGameObjectDataElement)unityAssetDataElement; 
            foreach (var assetUsagePointer in dataElement.EnumerateAssetUsages())
            {
                myGameObjectReferenceToControllerGuid.Add(assetUsagePointer.Location, assetUsagePointer.UsageTarget.ExternalAssetGuid);
            }
        }

        public void Invalidate()
        {
            myGameObjectReferenceToControllerGuid.Clear();
        }

        public Guid[] GetAnimatorsFromGameObject(List<LocalReference> gameObjectReferences)
        {
            myShellLocks.AssertReadAccessAllowed();
            var result = new List<Guid>();

            foreach (var gameObjectReference in gameObjectReferences)
            {
                if (myAssetDocumentHierarchyElementContainer.GetHierarchyElement(gameObjectReference, true) is
                    IComponentHierarchy he)
                {
                    var gameObjectHierarchy = myAssetDocumentHierarchyElementContainer.GetHierarchyElement(he.OwningGameObject, true);
                    if (gameObjectHierarchy is ImportedGameObjectHierarchy importedGameObjectHierarchy)
                    {
                        var importedGuids = importedGameObjectHierarchy.GetPrefabInstanceHierarchy().PrefabModifications
                            .Where(a => a.PropertyPath == UnityYamlConstants.ControllerProperty)
                            .Select(modification=> modification.ObjectReference)
                            .OfType<ExternalReference>()
                            .Select(objectReference => objectReference.ExternalAssetGuid)
                            .Distinct();
                        
                        result.AddRange(importedGuids);
                    }
                }

                var guids = myGameObjectReferenceToControllerGuid.Where(a => myAssetDocumentHierarchyElementContainer.GetHierarchyElement(a.Key, true) is
                    IComponentHierarchy h && h.OwningGameObject.Equals(gameObjectReference)).Select(a=>a.Value).Distinct();
                result.AddRange(guids);
            }

            return result.ToArray();
        }

        public LocalReference[] GetGameObjectReferencesByControllerGuid(Guid? controllerGuid)
        {
            if (!controllerGuid.HasValue) return EmptyArray<LocalReference>.Instance;
            
            //var gameObjectHierarchy = myAssetDocumentHierarchyElementContainer.GetHierarchyElement(he.OwningGameObject, true);
            // controllerGuid is just mentioned in one of the propertyPath: m_Controller of --- !u!1001 &788507972894748985, which we don't know
            
            var controllerReferences = myGameObjectReferenceToControllerGuid.Where(a => a.Value == controllerGuid).Select(a => a.Key);
            var t =  controllerReferences
                .Select(controllerReference => myAssetDocumentHierarchyElementContainer.GetHierarchyElement(controllerReference, true)).ToArray();
                return 
                    t
                .OfType<IComponentHierarchy>()
                .Select(h=>h.OwningGameObject).ToArray();
        }
    }
}