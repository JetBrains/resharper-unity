using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetUsages;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search
{
    public class UnityAssetReferenceSearcher : IDomainSpecificSearcher
    {
        private readonly DeferredCacheController myDeferredCacheController;
        private readonly AssetDocumentHierarchyElementContainer myAssetDocumentHierarchyElementContainer;
        private readonly AssetUsagesElementContainer myAssetUsagesElementContainer;
        private readonly UnityEventsElementContainer myUnityEventsElementContainer;
        private readonly AssetInspectorValuesContainer myAssetInspectorValuesContainer;
        private readonly IDeclaredElementsSet myElements;

        public UnityAssetReferenceSearcher(DeferredCacheController deferredCacheController, AssetDocumentHierarchyElementContainer assetDocumentHierarchyElementContainer,  AssetUsagesElementContainer assetUsagesElementContainer,
            UnityEventsElementContainer unityEventsElementContainer, AssetInspectorValuesContainer assetInspectorValuesContainer, MetaFileGuidCache metaFileGuidCache, IDeclaredElementsSet elements, bool findCandidates)
        {
            myDeferredCacheController = deferredCacheController;
            myAssetDocumentHierarchyElementContainer = assetDocumentHierarchyElementContainer;
            myAssetUsagesElementContainer = assetUsagesElementContainer;
            myUnityEventsElementContainer = unityEventsElementContainer;
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
                    var usages = myUnityEventsElementContainer.GetAssetUsagesFor(sourceFile, element);
                    foreach (var findResult in usages)
                    {
                        consumer.Accept(findResult);
                    }
                }

                if (element is ITypeElement typeElement)
                {
                    var usages = myAssetUsagesElementContainer.GetAssetUsagesFor(sourceFile, typeElement);

                    foreach (var assetUsage in usages)
                    {
                        consumer.Accept(new UnityScriptsFindResults(sourceFile, element, assetUsage, assetUsage.Location));
                    }
                }

                if (element is IField field)
                {
                    if (UnityApi.IsDescendantOfUnityEvent(field.Type.GetTypeElement()))
                    {
                        foreach (var findResult in myUnityEventsElementContainer.GetMethodsForUnityEvent(sourceFile, field))
                        {
                            consumer.Accept(findResult);
                        }
                    }
                    else
                    {
                        var usages = myAssetInspectorValuesContainer.GetAssetUsagesFor(sourceFile, field);
                        foreach (var variableUsage in usages)
                        {
                            consumer.Accept(new UnityInspectorFindResults(sourceFile, element, variableUsage, variableUsage.Location));
                        }
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