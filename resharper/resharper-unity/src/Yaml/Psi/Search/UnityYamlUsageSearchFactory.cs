using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
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
            bool findCandidates)
        {
            return elements.Any(IsInterestingElement)
                ? CreateSearcher(elements, findCandidates)
                : null;
        }

        private UnityAssetReferenceSearcher CreateSearcher(IDeclaredElementsSet elements, bool findCandidates)
        {
            var solution = elements.FirstOrDefault().NotNull("elements.FirstOrDefault() != null").GetSolution();
            var hierarchyContainer = solution.GetComponent<AssetDocumentHierarchyElementContainer>();
            var methodsContainer = solution.GetComponent<UnityEventsElementContainer>();
            var metaFileGuidCache = solution.GetComponent<MetaFileGuidCache>();
            var scriptsUsagesContainers = solution.GetComponent<IEnumerable<IScriptUsagesElementContainer>>();
            var assetValuesContainer = solution.GetComponent<AssetInspectorValuesContainer>();
            var controller = solution.GetComponent<DeferredCacheController>();
            
            return new UnityAssetReferenceSearcher(controller, hierarchyContainer, scriptsUsagesContainers, methodsContainer, assetValuesContainer, metaFileGuidCache, elements, findCandidates);
        }

        // Used to filter files before searching for references. Files must contain ANY of these search terms. An
        // ISearchGuru implementation can narrow the search domain further (e.g. checking for files that contain ALL of
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
            var settings = solution.GetSettingsStore();
            if (!settings.GetValue((UnitySettings key) => key.IsAssetIndexingEnabled))
                return false;
            
            var unityApi = element.GetSolution().TryGetComponent<UnityApi>();
            if (unityApi == null)
                return false;

            var assetSerializationMode = solution.GetComponent<AssetSerializationMode>();
            var yamlParsingEnabled = solution.GetComponent<AssetIndexingSupport>().IsEnabled;
            var deferredController = solution.GetComponent<DeferredCacheController>();

            
            if (!yamlParsingEnabled.Value || !assetSerializationMode.IsForceText || !deferredController.CompletedOnce.Value)
                return false;

            switch (element)
            {
                case IClass c:
                    return unityApi.IsUnityType(c);
                case IProperty _:
                case IMethod _:
                    return solution.GetComponent<UnityEventsElementContainer>().GetAssetUsagesCount(element, out var estimatedResult) > 0 || estimatedResult;
                case IField field:
                    return unityApi.IsSerialisedField(field);
            }

            return false;
        }
    }
}
