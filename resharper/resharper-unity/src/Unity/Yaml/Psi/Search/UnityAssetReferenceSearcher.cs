using System.Collections.Generic;
using JetBrains.Annotations;
using System;
using System.Linq;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Anim.Explicit;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Anim.Implicit;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues;
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
        private readonly ReferenceSearcherParameters myReferenceSearcherParameters;
        private readonly AnimExplicitUsagesContainer myAnimExplicitUsagesContainer;
        [NotNull] private readonly AnimImplicitUsagesContainer myAnimImplicitUsagesContainer;
        private readonly HashSet<IDeclaredElement> myOriginalElements;

        public UnityAssetReferenceSearcher(DeferredCacheController deferredCacheController,
                                           AssetDocumentHierarchyElementContainer assetDocumentHierarchyElementContainer,
                                           [NotNull, ItemNotNull] IEnumerable<IScriptUsagesElementContainer> scriptsUsagesElementContainers,
                                           UnityEventsElementContainer unityEventsElementContainer,
                                           [NotNull] AnimExplicitUsagesContainer animExplicitUsagesContainer,
                                           [NotNull] AnimImplicitUsagesContainer animImplicitUsagesContainer,
                                           AssetInspectorValuesContainer assetInspectorValuesContainer,
                                           MetaFileGuidCache metaFileGuidCache,
                                           IDeclaredElementsSet elements,
                                           ReferenceSearcherParameters referenceSearcherParameters)
        {
            myDeferredCacheController = deferredCacheController;
            myAssetDocumentHierarchyElementContainer = assetDocumentHierarchyElementContainer;
            myScriptsUsagesElementContainers = scriptsUsagesElementContainers;
            myUnityEventsElementContainer = unityEventsElementContainer;
            myAnimExplicitUsagesContainer = animExplicitUsagesContainer;
            myAnimImplicitUsagesContainer = animImplicitUsagesContainer;
            myAssetInspectorValuesContainer = assetInspectorValuesContainer;
            myElements = elements;
            myReferenceSearcherParameters = referenceSearcherParameters;

            var originalElements = myReferenceSearcherParameters.OriginalElements ?? myElements.ToList();
            myOriginalElements = new HashSet<IDeclaredElement>();

            foreach (var originalElement in originalElements)
            {
                myOriginalElements.Add(originalElement);
                if (originalElement is IProperty property)
                {
                    if (property.Getter != null)
                        myOriginalElements.Add(property.Getter);
                    
                    if (property.Setter != null)
                        myOriginalElements.Add(property.Setter);
                }
            }
        }

        public bool ProcessProjectItem<TResult>(IPsiSourceFile sourceFile, IFindResultConsumer<TResult> consumer)
        { 
            try
            {
                if (!myDeferredCacheController.CompletedOnce.Value)
                    return false;

                foreach (var element in myElements)
                {
                    if (element is IMethod or IProperty)
                    {
                        if (!myOriginalElements.Contains(element))
                            continue;
                        
                        var animImplicitUsages = myAnimImplicitUsagesContainer.GetUsagesFor(sourceFile, element);
                        foreach (var usage in animImplicitUsages)
                        {
                            var occurence = new AnimImplicitFindResult(sourceFile, element, usage);
                            consumer.Accept(occurence);
                        }

                        var animExplicitUsages = myAnimExplicitUsagesContainer.GetUsagesFor(sourceFile, element);
                        foreach (var usage in animExplicitUsages)
                        {
                            var occurence = new AnimExplicitFindResults(sourceFile, element, usage, usage.Location);
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