using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetMethods;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetUsages;
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
        private readonly MetaFileGuidCache myMetaFileGuidCache;
        private readonly IDeclaredElementsSet myElements;

        public UnityAssetReferenceSearcher(AssetDocumentHierarchyElementContainer assetDocumentHierarchyElementContainer,  AssetUsagesElementContainer assetUsagesElementContainer,
            AssetMethodsElementContainer assetMethodsElementContainer, MetaFileGuidCache metaFileGuidCache, IDeclaredElementsSet elements, bool findCandidates)
        {
            myAssetDocumentHierarchyElementContainer = assetDocumentHierarchyElementContainer;
            myAssetUsagesElementContainer = assetUsagesElementContainer;
            myAssetMethodsElementContainer = assetMethodsElementContainer;
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
                        var hierarchyElement =
                            myAssetDocumentHierarchyElementContainer.GetHierarchyElement(assetMethodData
                                .TargetScriptReference);
                        if (hierarchyElement != null)
                            consumer.Accept(new UnityMethodsFindResult(sourceFile, element, assetMethodData.TextRange,
                                hierarchyElement));
                    }
                }

                if (element is IClass)
                {
                    var elementSourceFile = element.GetSourceFiles().FirstOrDefault();
                    if (elementSourceFile == null)
                        return false;

                    var guid = myMetaFileGuidCache.GetAssetGuid(elementSourceFile);
                    if (guid == null)
                        return false;

                    var usages = myAssetUsagesElementContainer.GetAssetUsagesFor(sourceFile, guid);

                    foreach (var assetUsage in usages)
                    {
                        var hierarchyElement = myAssetDocumentHierarchyElementContainer.GetHierarchyElement(assetUsage.Location);
                        if (hierarchyElement == null)
                            continue;

                        consumer.Accept(new UnityScriptsFindResults(sourceFile, element, TextRange.InvalidRange, hierarchyElement));
                    }
                }

                if (element is IField field)
                {
                    // TODO
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