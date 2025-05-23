using System.Collections.Generic;
using System.Linq;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.DeferredCaches;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Anim.Explicit;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Anim.Implicit;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Impl.Search.SearchDomain;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search
{
    // Note that there isn't a default searcher factory for YAML because it has no declared elements to search for. This
    // searcher factory is looking for C# declared elements - class or method, etc.
    [PsiSharedComponent]
    public class UnityYamlUsageSearchFactory : DomainSpecificSearcherFactoryBase
    {
        private readonly SearchDomainFactory mySearchDomainFactory;

        public UnityYamlUsageSearchFactory(SearchDomainFactory searchDomainFactory)
        {
            mySearchDomainFactory = searchDomainFactory;
        }

        public override bool IsCompatibleWithLanguage(PsiLanguageType languageType)
        {
            return languageType.Is<UnityYamlLanguage>();
        }

        public override IDomainSpecificSearcher CreateReferenceSearcher(IDeclaredElementsSet elements,
            ReferenceSearcherParameters referenceSearcherParameters)
        {
            return elements.Any(IsInterestingElement)
                ? CreateSearcher(elements, referenceSearcherParameters)
                : null;
        }

        private UnityAssetReferenceSearcher CreateSearcher(IDeclaredElementsSet elements, ReferenceSearcherParameters referenceSearcherParameters)
        {
            var solution = elements.FirstOrDefault().NotNull("elements.FirstOrDefault() != null").GetSolution();
            var hierarchyContainer = solution.GetComponent<AssetDocumentHierarchyElementContainer>();
            var methodsContainer = solution.GetComponent<UnityEventsElementContainer>();
            var metaFileGuidCache = solution.GetComponent<MetaFileGuidCache>();
            var scriptsUsagesContainers = solution.GetComponent<IEnumerable<IScriptUsagesElementContainer>>();
            var animExplicitUsagesContainer = solution.GetComponent<AnimExplicitUsagesContainer>();
            var animImplicitUsagesContainer = solution.GetComponent<AnimImplicitUsagesContainer>();
            var assetValuesContainer = solution.GetComponent<AssetInspectorValuesContainer>();
            var controller = solution.GetComponent<DeferredCacheController>();

            return new UnityAssetReferenceSearcher(controller, scriptsUsagesContainers,
                methodsContainer, animExplicitUsagesContainer, animImplicitUsagesContainer,
                assetValuesContainer, elements,
                referenceSearcherParameters);
        }

        // Used to filter files before searching for references. Files must contain ANY of these search terms. An
        // ISearchFilter implementation can narrow the search domain further (e.g. checking for files that contain ALL of
        // the terms). Method references require the element short name, while class references require the class's
        // file's asset guid
        public override IEnumerable<string> GetAllPossibleWordsInFile(IDeclaredElement element)
        {
            if (IsInterestingElement(element))
            {
                var words = new List<string> {UnityAssetTrigramIndexBuild.ASSET_REFERENCE_IDENTIFIER};
                return words;
            }

            return EmptyList<string>.Instance;
        }

        public override ISearchDomain GetDeclaredElementSearchDomain(IDeclaredElement declaredElement)
        {
            if (IsInterestingElement(declaredElement))
            {
                var moduleFactory = declaredElement.GetSolution().TryGetComponent<UnityExternalFilesModuleFactory>();
                if (moduleFactory != null)
                    return mySearchDomainFactory.CreateSearchDomain(moduleFactory.PsiModule);
            }

            return EmptySearchDomain.Instance;
        }

        public static bool IsInterestingElement(IDeclaredElement element)
        {
            var solution = element.GetSolution();

            var unityApi = solution.TryGetComponent<UnityApi>();
            var isForceText = solution.GetComponent<AssetSerializationMode>().IsForceText;
            var assetIndexingEnabled = solution.GetComponent<AssetIndexingSupport>().IsEnabled.Value;
            var deferredCachesCompleted = solution.GetComponent<DeferredCacheController>().CompletedOnce.Value;

            if (unityApi == null || !assetIndexingEnabled || !isForceText || !deferredCachesCompleted)
                return false;

            switch (element)
            {
                case IClass c:
                    return unityApi.IsUnityType(c);

                case IProperty _:
                case IMethod _:
                    if (unityApi.IsSerialisedAutoProperty(element as IProperty, true)
                        .HasFlag(SerializedFieldStatus.UnitySerializedField))
                        return true;
                    var eventsCount = solution
                        .GetComponent<UnityEventsElementContainer>()
                        .GetAssetUsagesCount(element, out var unityEventsEstimatedResult);
                    var animationEventsCount = solution
                        .GetComponent<AnimExplicitUsagesContainer>()
                        .GetEventUsagesCountFor(element, out var animationEventsEstimatedResult);
                    var animationImplicitEventsCount = solution
                        .GetComponent<AnimImplicitUsagesContainer>()
                        .GetEventUsagesCountFor(element, out var animationImplicitEventsEstimatedResult);
                    var count = eventsCount + animationEventsCount + animationImplicitEventsCount;
                    return count > 0 || unityEventsEstimatedResult || animationEventsEstimatedResult || animationImplicitEventsEstimatedResult;

                case IField field:
                    return unityApi.IsSerialisedField(field).HasFlag(SerializedFieldStatus.UnitySerializedField);
            }

            return false;
        }
    }
}
