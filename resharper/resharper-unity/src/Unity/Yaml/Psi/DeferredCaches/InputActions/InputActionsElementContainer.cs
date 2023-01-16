using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Search;
using JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.DeclaredElements;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetScriptUsages;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.InputActions
{
    // UnityEventsElementContainer stores whole index in memory, it is not expected to have millions of methods. In case of high memory usage
    // with millions of methods, only pointers to AssetMethodData should be stored. AssetMethodData will be deserialized only in find usages,
    // strings should be replaced by int hashes.
    // Information about imported/prefab modifications could be stored in memory, it should not allocate a lot of memory ever.
    [SolutionComponent]
    public class InputActionsElementContainer : IUnityAssetDataElementContainer
    {
        private const string PlayerInputGuid = "62899f850307741f2a39c98a8b639597";

        private readonly IShellLocks myShellLocks;
        private readonly ILogger myLogger; 
        private readonly OneToListMap<IPsiSourceFile, PlayerInputUsage> myElementsWithPlayerInputReference = new();

        public string Id => nameof(InputActionsElementContainer);
        public int Order => 0;

        public InputActionsElementContainer(IShellLocks shellLocks, ILogger logger)
        {
            myShellLocks = shellLocks;
            myLogger = logger;
        }
        
        public IUnityAssetDataElement CreateDataElement(IPsiSourceFile sourceFile)
        {
            return new InputActionsDataElement();
        }

        public bool IsApplicable(IPsiSourceFile currentAssetSourceFile)
        {
            return currentAssetSourceFile.IsScene() || currentAssetSourceFile.IsPrefab();
        }

        public object Build(IPsiSourceFile currentAssetSourceFile, AssetDocument assetDocument)
        {
            var result = new LocalList<PlayerInputUsage>();
            if (ourPlayerInputSearcher.Find(assetDocument.Buffer) >= 0)
            {
                var anchorRaw = AssetUtils.GetAnchorFromBuffer(assetDocument.Buffer);
                var stripped = AssetUtils.IsStripped(assetDocument.Buffer);
                if (stripped) // we will handle it in prefabs
                    return null;

                if (!anchorRaw.HasValue)
                    return null;
                
                if (AssetUtils.GetInputActionsReference(currentAssetSourceFile, assetDocument.Buffer) is ExternalReference
                    inputActionsReference)
                {
                    var inputActionsGuid = inputActionsReference.ExternalAssetGuid;
                    
                    if (AssetUtils.GetGameObjectReference(currentAssetSourceFile, assetDocument.Buffer) is LocalReference
                        gameObjectLocalReference)
                    {
                        result.Add(new PlayerInputUsage(gameObjectLocalReference, inputActionsGuid));
                    }
                }
            }

            return result;
        }
        
        private static readonly StringSearcher ourPlayerInputSearcher = new(PlayerInputGuid, false);

        public void Drop(IPsiSourceFile currentAssetSourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement,
            IUnityAssetDataElement unityAssetDataElement)
        { 
            myElementsWithPlayerInputReference.RemoveKey(currentAssetSourceFile);
        }

        public void Merge(IPsiSourceFile currentAssetSourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement,
            IUnityAssetDataElementPointer unityAssetsCache, IUnityAssetDataElement unityAssetDataElement)
        {
            if (unityAssetDataElement is not InputActionsDataElement dataElement) return;
            myElementsWithPlayerInputReference.AddValueRange(currentAssetSourceFile, dataElement.Usages);
        }

        public void Invalidate()
        {
            myElementsWithPlayerInputReference.Clear();
        }
        
        public int GetUsagesCountForFast(IDeclaredElement el, out bool estimated)
        {
            myShellLocks.AssertReadAccessAllowed();

            estimated = false;
            if (!UnityInputActionsReferenceUsageSearchFactory.IsInterestingElement(el))
                return 0;

            estimated = true;
            return 1;
        }

        // GetUsagesCountForFast is fast but inaccurate, GetUsagesCountFor is accurate, but slow
        // public int GetUsagesCountFor(IDeclaredElement el, out bool estimated)
        // {
        //     myShellLocks.AssertReadAccessAllowed();
        //     estimated = false;
        //     var usages = GetUsagesFor(el);
        //     if (usages.Length > 0)
        //         estimated = true;
        //     return usages.Length;
        // }

        
        // inputActionsFile-s are pre-filtered by word-index in CSharpInputActionsReferenceSearcher,
        // it means that all inputActionsFile have the methodName in question.
        public InputActionsDeclaredElement[] GetUsagesFor(IPsiSourceFile inputActionsFile, IDeclaredElement el)
        {
            myShellLocks.AssertReadAccessAllowed();

            if (!UnityInputActionsReferenceUsageSearchFactory.IsInterestingElement(el))
                return Array.Empty<InputActionsDeclaredElement>();
            if (el is not IMethod method) return Array.Empty<InputActionsDeclaredElement>();
            var strippedMethodName = method.ShortName.Substring(2);
            var type = method.ContainingType;
            if (type is not IClass classType) return Array.Empty<InputActionsDeclaredElement>();

            var solution = el.GetSolution();
            var container = solution.GetComponent<AssetScriptUsagesElementContainer>();
            var hierarchyElementContainer = solution.GetComponent<AssetDocumentHierarchyElementContainer>();
            var inputActionsCache = solution.GetComponent<InputActionsCache>();
            var metaFileGuidCache = solution.GetComponent<MetaFileGuidCache>();

            var inputActionsFileGuid = metaFileGuidCache.GetAssetGuid(inputActionsFile);
            var possibleElementsWithPlayerInputReference = myElementsWithPlayerInputReference
                .Values
                .Where(usage => usage.InputActionsFileGuid == inputActionsFileGuid)
                .SelectMany(a =>
            {
                var results = new List<LocalReference>();
                GetSelfAndOriginalGameObjects(a.Location, hierarchyElementContainer, results);
                return results.Select(item => new PlayerInputUsage(item, a.InputActionsFileGuid));
            }).ToArray();

            var possibleScriptLocalReference = container.GetPossibleFilesWithScriptUsages(classType).ToArray()
                .SelectMany(a => container.GetScriptUsagesFor(a, classType)).ToArray()
                .SelectMany(a =>
                {
                    var results = new List<LocalReference>();
                    GetSelfAndOriginalGameObjects(a.Location, hierarchyElementContainer, results);
                    return results;
                }).ToArray();

            return possibleScriptLocalReference.SelectMany(scriptUsageLocation =>
            {
                var playerInputUsages = possibleElementsWithPlayerInputReference
                    .Where(t => t.Location.Equals(scriptUsageLocation)).ToArray();

                return playerInputUsages.SelectMany(a =>
                    metaFileGuidCache.GetAssetFilePathsFromGuid(a.InputActionsFileGuid).SelectMany(path =>
                        inputActionsCache.GetDeclaredElements(path, strippedMethodName))).ToArray();
            }).ToArray();
        }
        
        private static void GetSelfAndOriginalGameObjects(LocalReference reference, AssetDocumentHierarchyElementContainer hierarchyElementContainer, ICollection<LocalReference> results)
        {
            var he = hierarchyElementContainer.GetHierarchyElement(reference, true);
            if (he is ImportedGameObjectHierarchy importedGameObjectHierarchy)
            {
                GetSelfAndOriginalGameObjects(importedGameObjectHierarchy.OriginalGameObject.Location, hierarchyElementContainer, results);
            }
            else if (he is ScriptComponentHierarchy scriptComponentHierarchy)
            {
                GetSelfAndOriginalGameObjects(scriptComponentHierarchy.OwningGameObject, hierarchyElementContainer, results);
            }

            results.Add(reference);
        }
    }
}