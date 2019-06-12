using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Search;
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
                ? new YamlReferenceSearcher(this, elements, findCandidates)
                : null;
        }

        // Used to filter files before searching for references. Files must contain ANY of these search terms. An
        // ISearchGuru implementation can narrow the search domain further (e.g. checking for files that contain ALL of
        // the terms). Method references require the element short name, while class references require the class's
        // file's asset guid
        public override IEnumerable<string> GetAllPossibleWordsInFile(IDeclaredElement element)
        {
            if (IsInterestingElement(element))
            {
                var words = new List<string>();

                // If it's a class, we only need the asset GUID
                if (element is IClass)
                {
                    var metaFileGuidCache = element.GetSolution().GetComponent<MetaFileGuidCache>();
                    foreach (var sourceFile in element.GetSourceFiles())
                    {
                        // If the element doesn't have the same name as the file it's in, Unity doesn't recognise it
                        if (!sourceFile.Name.StartsWith(element.ShortName))
                            continue;

                        var guid = metaFileGuidCache.GetAssetGuid(sourceFile);
                        if (guid != null)
                            words.Add(guid);
                    } 
                }
                else
                {
                    words.Add(element.ShortName);
                }

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

        private static bool IsInterestingElement(IDeclaredElement element)
        {
            var unityApi = element.GetSolution().TryGetComponent<UnityApi>();
            if (unityApi == null)
                return false;
            return unityApi.IsUnityType(element as IClass)
                   || unityApi.IsPotentialEventHandler(element as IMethod)
                   || unityApi.IsPotentialEventHandler(element as IProperty);
        }
    }
}
