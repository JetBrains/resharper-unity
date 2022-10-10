using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
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
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
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
// new Dict<GUID, List<LocalReference>>
        private readonly List<PlayerInputUsage> myElementsWithPlayerInputReference = new(); 
        
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
            return currentAssetSourceFile.IsScene();
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

                var mActions = assetDocument.Document.GetUnityObjectPropertyValue<INode>(UnityYamlConstants.Actions);
                var inputActionsGuid = ((IFlowMappingNode)mActions).GetMapEntryPlainScalarText("guid").NotNull("guid != null");

                if (AssetUtils.GetGameObjectReference(currentAssetSourceFile, assetDocument.Buffer) is LocalReference
                    gameObjectLocalReference)
                {
                    result.Add(new PlayerInputUsage(gameObjectLocalReference, Guid.Parse(inputActionsGuid)));
                }
            }

            return result;
        }
        
        // todo: use JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.InputActions.PlayerInputUsage.PlayerInputGuid
        private static readonly StringSearcher ourPlayerInputSearcher = new(PlayerInputGuid, false);

        public void Drop(IPsiSourceFile currentAssetSourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement,
            IUnityAssetDataElement unityAssetDataElement)
        {
            var dataElement = unityAssetDataElement as InputActionsDataElement;
            foreach (var usage in dataElement.Usages)
            {
                myElementsWithPlayerInputReference.Remove(usage);
            }
        }

        public void Merge(IPsiSourceFile currentAssetSourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement,
            IUnityAssetDataElementPointer unityAssetsCache, IUnityAssetDataElement unityAssetDataElement)
        {
            if (unityAssetDataElement is not InputActionsDataElement dataElement) return;
            foreach (var usage in dataElement.Usages)
            {
                myElementsWithPlayerInputReference.Add(usage);
            }
        }

        public void Invalidate()
        {
            myElementsWithPlayerInputReference.Clear();
        }
        
        // GetGameObjectsWithInputActions()

        public int GetUsagesCountFor(IDeclaredElement el, out bool estimated)
        {
            estimated = false;
            var usages = GetUsagesFor(el);
            if (usages.Length > 0)
                estimated = true;
            return usages.Length;
        }

        private static void GetSelfAndOriginalGameObjects(LocalReference reference, AssetDocumentHierarchyElementContainer hierarchyElementContainer, ICollection<LocalReference> results)
        {
            var he = hierarchyElementContainer.GetHierarchyElement(reference, true); 
            if (he is ImportedGameObjectHierarchy importedGameObjectHierarchy)
            {
                GetSelfAndOriginalGameObjects(importedGameObjectHierarchy.OriginalGameObject.Location, hierarchyElementContainer, results);
            }

            results.Add(reference);
        }
        
        public InputActionsDeclaredElement[] GetUsagesFor(IDeclaredElement el)
        {
            if (!UnityInputActionsReferenceUsageSearchFactory.IsInterestingElement(el)) return Array.Empty<InputActionsDeclaredElement>();
            var method = (IMethod)el;
            var strippedMethodName = method.ShortName.Substring(2);
            var type = method.ContainingType;
            if (type is not IClass classType) return Array.Empty<InputActionsDeclaredElement>();
            
            var solution = el.GetSolution();
            var container = solution.GetComponent<AssetScriptUsagesElementContainer>();
            var hierarchyElementContainer = solution.GetComponent<AssetDocumentHierarchyElementContainer>();
            var inputActionsCache = solution.GetComponent<InputActionsCache>();
            var metaFileGuidCache = solution.GetComponent<MetaFileGuidCache>();

            var possibleElementsWithPlayerInputReference = myElementsWithPlayerInputReference.SelectMany(a =>
            {
                var results = new List<LocalReference>();
                GetSelfAndOriginalGameObjects(a.Location, hierarchyElementContainer, results);
                return results.Select(item => new PlayerInputUsage(item, a.InputActionsFileGuid));
            }).ToArray();
            
            foreach (var sf in container.GetPossibleFilesWithScriptUsages(classType))
            {
                var usages = container.GetScriptUsagesFor(sf, classType);
                foreach (var scriptUsage in usages)
                {
                    var element = hierarchyElementContainer.GetHierarchyElement(scriptUsage.Location, true);

                    if (element is not IScriptComponentHierarchy script) continue;
                    
                    var localReference = new LocalReference(script.OwningGameObject.OwningPsiPersistentIndex, script.OwningGameObject.LocalDocumentAnchor);
                    var playerInputUsages = possibleElementsWithPlayerInputReference.Where(t => t.Location.Equals(localReference)).ToArray();

                    return playerInputUsages.SelectMany(a =>
                        metaFileGuidCache.GetAssetFilePathsFromGuid(a.InputActionsFileGuid).SelectMany(path =>
                            inputActionsCache.GetDeclaredElements(path, strippedMethodName))).ToArray();
                }
            }

            return Array.Empty<InputActionsDeclaredElement>();
        }
    }
}