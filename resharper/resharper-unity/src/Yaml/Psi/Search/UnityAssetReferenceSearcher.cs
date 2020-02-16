using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetMethods;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Search;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search
{
    public class UnityAssetReferenceSearcher : IDomainSpecificSearcher
    {
        private readonly AssetDocumentHierarchyElementContainer myAssetDocumentHierarchyElementContainer;
        private readonly AssetMethodsElementContainer myAssetMethodsElementContainer;
        private readonly IDeclaredElementsSet myElements;

        public UnityAssetReferenceSearcher(AssetDocumentHierarchyElementContainer assetDocumentHierarchyElementContainer, 
            AssetMethodsElementContainer assetMethodsElementContainer, IDeclaredElementsSet elements, bool findCandidates)
        {
            myAssetDocumentHierarchyElementContainer = assetDocumentHierarchyElementContainer;
            myAssetMethodsElementContainer = assetMethodsElementContainer;
            myElements = elements;
        }

        public bool ProcessProjectItem<TResult>(IPsiSourceFile sourceFile, IFindResultConsumer<TResult> consumer)
        {
            foreach (var element in myElements)
            {
                var usages = myAssetMethodsElementContainer.GetAssetUsagesFor(sourceFile, element);
                foreach (var assetMethodData in usages)
                {
                    var hierarchyElement = myAssetDocumentHierarchyElementContainer.GetHierarchyElement(assetMethodData.TargetScriptReference);
                    if (hierarchyElement != null)
                        consumer.Accept(new UnityAssetFindResult(sourceFile, element, assetMethodData.TextRange,
                            hierarchyElement));
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