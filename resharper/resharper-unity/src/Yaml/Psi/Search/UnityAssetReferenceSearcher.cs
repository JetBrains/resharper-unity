using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetMethods;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetUsages;
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
        private readonly AssetDocumentHierarchyElementContainer myAssetDocumentHierarchyElementContainer;
        private readonly AssetUsagesElementContainer myAssetUsagesElementContainer;
        private readonly AssetMethodsElementContainer myAssetMethodsElementContainer;
        private readonly AssetInspectorValuesContainer myAssetInspectorValuesContainer;
        private readonly MetaFileGuidCache myMetaFileGuidCache;
        private readonly IDeclaredElementsSet myElements;

        public UnityAssetReferenceSearcher(AssetDocumentHierarchyElementContainer assetDocumentHierarchyElementContainer,  AssetUsagesElementContainer assetUsagesElementContainer,
            AssetMethodsElementContainer assetMethodsElementContainer, AssetInspectorValuesContainer assetInspectorValuesContainer, MetaFileGuidCache metaFileGuidCache, IDeclaredElementsSet elements, bool findCandidates)
        {
            myAssetDocumentHierarchyElementContainer = assetDocumentHierarchyElementContainer;
            myAssetUsagesElementContainer = assetUsagesElementContainer;
            myAssetMethodsElementContainer = assetMethodsElementContainer;
            myAssetInspectorValuesContainer = assetInspectorValuesContainer;
            myMetaFileGuidCache = metaFileGuidCache;
            myElements = elements;
        }

        public bool ProcessProjectItem<TResult>(IPsiSourceFile sourceFile, IFindResultConsumer<TResult> consumer)
        {
            foreach (var element in myElements)
            {
                if (element is IMethod || element is IProperty)
                {
                    var usages = myAssetMethodsElementContainer.GetAssetUsagesFor(sourceFile, element);
                    foreach (var assetMethodData in usages)
                    {
                        var hierarchyElement = myAssetDocumentHierarchyElementContainer.GetHierarchyElement(assetMethodData.Location, false);
                        if (hierarchyElement != null)
                            consumer.Accept(new UnityMethodsFindResult(sourceFile, element, assetMethodData, hierarchyElement));
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

                        consumer.Accept(new UnityScriptsFindResults(sourceFile, element, assetUsage, hierarchyElement));
                    }
                }

                if (element is IField field)
                {
                    var usages = myAssetInspectorValuesContainer.GetAssetUsagesFor(sourceFile, field);
                    foreach (var assetUsage in usages)
                    {
                        var hierarchyElement = myAssetDocumentHierarchyElementContainer.GetHierarchyElement(assetUsage.Location, false);
                        if (hierarchyElement == null)
                            continue;

                        consumer.Accept(new UnityInspectorFindResults(sourceFile, element, assetUsage, hierarchyElement));
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