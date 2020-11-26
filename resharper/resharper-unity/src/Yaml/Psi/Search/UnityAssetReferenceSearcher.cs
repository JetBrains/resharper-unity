using System;
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
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search
{
    public class UnityAssetReferenceSearcher : IDomainSpecificSearcher
    {
        private static readonly ILogger ourLogger = Logger.GetLogger(nameof(UnityAssetReferenceSearcher));
        
        private readonly DeferredCacheController myDeferredCacheController;
        private readonly AssetDocumentHierarchyElementContainer myAssetDocumentHierarchyElementContainer;
        private readonly AssetScriptUsagesElementContainer myAssetScriptUsagesElementContainer;
        private readonly UnityEventsElementContainer myUnityEventsElementContainer;
        private readonly AssetInspectorValuesContainer myAssetInspectorValuesContainer;
        private readonly IDeclaredElementsSet myElements;

        public UnityAssetReferenceSearcher(DeferredCacheController deferredCacheController, AssetDocumentHierarchyElementContainer assetDocumentHierarchyElementContainer,  AssetScriptUsagesElementContainer assetScriptUsagesElementContainer,
            UnityEventsElementContainer unityEventsElementContainer, AssetInspectorValuesContainer assetInspectorValuesContainer, MetaFileGuidCache metaFileGuidCache, IDeclaredElementsSet elements, bool findCandidates)
        {
            myDeferredCacheController = deferredCacheController;
            myAssetDocumentHierarchyElementContainer = assetDocumentHierarchyElementContainer;
            myAssetScriptUsagesElementContainer = assetScriptUsagesElementContainer;
            myUnityEventsElementContainer = unityEventsElementContainer;
            myAssetInspectorValuesContainer = assetInspectorValuesContainer;
            myElements = elements;
        }

        public bool ProcessProjectItem<TResult>(IPsiSourceFile sourceFile, IFindResultConsumer<TResult> consumer)
        {
            try
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
                        var usages = myAssetScriptUsagesElementContainer.GetAssetUsagesFor(sourceFile, typeElement);

                        foreach (var assetUsage in usages)
                        {
                            consumer.Accept(new UnityScriptsFindResults(sourceFile, element, assetUsage,
                                assetUsage.Location));
                        }
                    }

                    if (element is IField field)
                    {
                        if (field.Type.GetTypeElement().DerivesFromUnityEvent())
                        {
                            foreach (var findResult in myUnityEventsElementContainer.GetMethodsForUnityEvent(sourceFile,
                                field))
                            {
                                consumer.Accept(findResult);
                            }
                        }
                        else
                        {
                            var usages = myAssetInspectorValuesContainer.GetAssetUsagesFor(sourceFile, field);
                            foreach (var findResult in usages)
                            {
                                consumer.Accept(findResult);
                            }
                        }
                    }
                }

            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                ourLogger.Error(e, $"An error occurred while searching assets in: {sourceFile.GetPersistentIdForLogging()}");
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