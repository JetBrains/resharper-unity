using System.Collections.Generic;
using JetBrains.Annotations;
using System;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimationEventsUsages;
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
        [NotNull, ItemNotNull] private readonly IEnumerable<IScriptUsagesElementContainer> myScriptsUsagesElementContainers;
        private readonly UnityEventsElementContainer myUnityEventsElementContainer;
        private readonly AssetInspectorValuesContainer myAssetInspectorValuesContainer;
        private readonly IDeclaredElementsSet myElements;
        private readonly AnimationEventUsagesContainer myAnimationEventUsagesContainer;

        public UnityAssetReferenceSearcher(DeferredCacheController deferredCacheController,
                                           AssetDocumentHierarchyElementContainer assetDocumentHierarchyElementContainer,
                                           [NotNull, ItemNotNull] IEnumerable<IScriptUsagesElementContainer> scriptsUsagesElementContainers,
                                           UnityEventsElementContainer unityEventsElementContainer,
                                           [NotNull] AnimationEventUsagesContainer animationEventUsagesContainer,
                                           AssetInspectorValuesContainer assetInspectorValuesContainer,
                                           MetaFileGuidCache metaFileGuidCache,
                                           IDeclaredElementsSet elements,
                                           bool findCandidates)
        {
            myDeferredCacheController = deferredCacheController;
            myAssetDocumentHierarchyElementContainer = assetDocumentHierarchyElementContainer;
            myScriptsUsagesElementContainers = scriptsUsagesElementContainers;
            myUnityEventsElementContainer = unityEventsElementContainer;
            myAnimationEventUsagesContainer = animationEventUsagesContainer;
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
                        var animationEventUsages = myAnimationEventUsagesContainer.GetEventUsagesFor(sourceFile, element);
                        foreach (var usage in animationEventUsages)
                        {
                            var occurence = new UnityAnimationEventFindResults(sourceFile, element, usage, usage.Location);
                            consumer.Accept(occurence);
                        }

                        var usages = myUnityEventsElementContainer.GetAssetUsagesFor(sourceFile, element);
                        foreach (var findResult in usages)
                        {
                            consumer.Accept(findResult);
                        }
                    }

                    if (element is ITypeElement typeElement)
                    {
                        AddScriptUsages(sourceFile, consumer, typeElement, element);
                    }

                    if (element is IField field)
                    {
                        if (field.Type.GetTypeElement().DerivesFromUnityEvent())
                        {
                            foreach (var findResult in myUnityEventsElementContainer.GetMethodsForUnityEvent(sourceFile, field))
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

        private void AddScriptUsages<TResult>([NotNull] IPsiSourceFile sourceFile,
                                              [NotNull] IFindResultConsumer<TResult> consumer,
                                              [NotNull] ITypeElement typeElement,
                                              [NotNull] IDeclaredElement element)
        {
            foreach (var scriptUsagesContainer in myScriptsUsagesElementContainers)
            {
                var scriptUsages = scriptUsagesContainer.GetScriptUsagesFor(sourceFile, typeElement);
                foreach (var scriptUsage in scriptUsages)
                {
                    consumer.Accept(new UnityScriptsFindResults(sourceFile, element, scriptUsage, scriptUsage.Location));
                }
            }
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