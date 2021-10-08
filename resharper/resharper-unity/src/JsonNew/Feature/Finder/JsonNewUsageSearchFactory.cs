using System.Collections.Generic;
using System.Linq;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Impl.Search.SearchDomain;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Feature.Finder
{
    [PsiSharedComponent]
    public class JsonNewUsageSearchFactory : DomainSpecificSearcherFactoryBase
    {
        private readonly SearchDomainFactory mySearchDomainFactory;

        public JsonNewUsageSearchFactory(SearchDomainFactory searchDomainFactory)
        {
            mySearchDomainFactory = searchDomainFactory;
        }

        public override bool IsCompatibleWithLanguage(PsiLanguageType languageType)
        {
            return languageType.Is<JsonNewLanguage>();
        }

        public override IDomainSpecificSearcher CreateReferenceSearcher(IDeclaredElementsSet elements,
            bool findCandidates)
        {
            return elements.Any(IsInterestingElement)
                ? new JsonNewReferenceSearcher(this, elements, findCandidates)
                : null;
        }

        public override IEnumerable<string> GetAllPossibleWordsInFile(IDeclaredElement element)
        {
            if (IsInterestingElement(element))
            {
                return new [] {element.ShortName};
            }

            return EmptyList<string>.Instance;
        }

        public override ISearchDomain GetDeclaredElementSearchDomain(IDeclaredElement declaredElement)
        {
            if (IsInterestingElement(declaredElement))
            {
                var element = (declaredElement as IJsonSearchDomainOwner).NotNull("element != null");
                return element.GetSearchDomain(mySearchDomainFactory);
            }

            return EmptySearchDomain.Instance;
        }

        private static bool IsInterestingElement(IDeclaredElement element)
        {
            return element is JsonNewDeclaredElementBase;
        }
    }
}
