using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetMethods;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetUsages;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Search;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search
{
    public class UnityAssetReferenceSearcher : IDomainSpecificSearcher
    {
        private readonly DeferredCacheController myDeferredCacheController;
        private readonly UnityInterningCache myUnityInterningCache;
        private readonly AssetDocumentHierarchyElementContainer myAssetDocumentHierarchyElementContainer;
        private readonly AssetUsagesElementContainer myAssetUsagesElementContainer;
        private readonly AssetMethodsElementContainer myAssetMethodsElementContainer;
        private readonly AssetInspectorValuesContainer myAssetInspectorValuesContainer;
        private readonly IDeclaredElementsSet myElements;

        public UnityAssetReferenceSearcher(DeferredCacheController deferredCacheController, UnityInterningCache unityInterningCache, AssetDocumentHierarchyElementContainer assetDocumentHierarchyElementContainer,  AssetUsagesElementContainer assetUsagesElementContainer,
            AssetMethodsElementContainer assetMethodsElementContainer, AssetInspectorValuesContainer assetInspectorValuesContainer, MetaFileGuidCache metaFileGuidCache, IDeclaredElementsSet elements, bool findCandidates)
        {
            myDeferredCacheController = deferredCacheController;
            myUnityInterningCache = unityInterningCache;
            myAssetDocumentHierarchyElementContainer = assetDocumentHierarchyElementContainer;
            myAssetUsagesElementContainer = assetUsagesElementContainer;
            myAssetMethodsElementContainer = assetMethodsElementContainer;
            myAssetInspectorValuesContainer = assetInspectorValuesContainer;
            myElements = elements;
        }

        public bool ProcessProjectItem<TResult>(IPsiSourceFile sourceFile, IFindResultConsumer<TResult> consumer)
        {
            if (!myDeferredCacheController.CompletedOnce.Value)
                return false;
            
            foreach (var element in myElements)
            {
                if (element is IMethod || element is IProperty)
                {
                    var usages = myAssetMethodsElementContainer.GetAssetUsagesFor(sourceFile, element);
                    foreach (var assetMethodData in usages)
                    {
                        var hierarchyElement = myAssetDocumentHierarchyElementContainer.GetHierarchyElement(assetMethodData.Location, false);
                        if (hierarchyElement != null)
                            consumer.Accept(new UnityMethodsFindResult(sourceFile, element, assetMethodData, hierarchyElement, hierarchyElement.GetLocation(myUnityInterningCache)));
                    }
                }

                if (element is ITypeElement typeElement)
                {
                    var usages = myAssetUsagesElementContainer.GetAssetUsagesFor(sourceFile, typeElement);

                    foreach (var assetUsage in usages)
                    {
                        var hierarchyElement = myAssetDocumentHierarchyElementContainer.GetHierarchyElement(assetUsage.Location, false);
                        if (hierarchyElement == null)
                            continue;

                        consumer.Accept(new UnityScriptsFindResults(sourceFile, element, assetUsage, hierarchyElement, hierarchyElement.GetLocation(myUnityInterningCache)));
                    }
                }

                if (element is IField field)
                {
                    var usages = myAssetInspectorValuesContainer.GetAssetUsagesFor(sourceFile, field);
                    foreach (var variableUsage in usages)
                    {
                        var location = myUnityInterningCache.GetReference<LocalReference>(variableUsage.Location);
                        var hierarchyElement = myAssetDocumentHierarchyElementContainer.GetHierarchyElement(location, false);
                        if (hierarchyElement == null)
                            continue;

                        consumer.Accept(new UnityInspectorFindResults(sourceFile, element, variableUsage, hierarchyElement, hierarchyElement.GetLocation(myUnityInterningCache)));
                    }
                }
            }

            return false;
        }

        public bool ProcessElement<TResult>(ITreeNode element, IFindResultConsumer<TResult> consumer)
        {
            var sf = element.GetSourceFile();
            if (sf != null)
                ProcessProjectItem(sf, consumer);
            return false;
        }
    }
}