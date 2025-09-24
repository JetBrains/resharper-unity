using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Impl.Search.SearchDomain;
using JetBrains.ReSharper.Psi.Search;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Search
{
    [PsiSharedComponent]
    public class ShaderLabUsageSearchFactory : DomainSpecificSearcherFactoryBase
    {
        private readonly SearchDomainFactory mySearchDomainFactory;

        public ShaderLabUsageSearchFactory(SearchDomainFactory searchDomainFactory)
        {
            mySearchDomainFactory = searchDomainFactory;
        }

        public override bool IsCompatibleWithLanguage(PsiLanguageType languageType) => languageType.Is<ShaderLabLanguage>() || languageType.Is<CSharpLanguage>();

        public override IDomainSpecificSearcher CreateReferenceSearcher(IDeclaredElementsSet elements, ReferenceSearcherParameters referenceSearcherParameters)
        {
            if (elements.Any(element => element is not IShaderLabDeclaredElement))
                return null;
            return new ShaderLabReferenceSearcher(elements, referenceSearcherParameters);
        }

        public override IDomainSpecificSearcher CreateTextOccurrenceSearcher(IDeclaredElementsSet elements, TextOccurrenceSearcherParameters parameters)
        {
            return new ShaderLabTextOccurrenceSearcher(elements, parameters);
        }

        public override IDomainSpecificSearcher CreateTextOccurrenceSearcher(string subject, TextOccurrenceSearcherParameters parameters)
        {
            return new ShaderLabTextOccurrenceSearcher(subject, parameters);
        }

        public override IEnumerable<string> GetAllPossibleWordsInFile(IDeclaredElement element)
        {
            yield return element.ShortName;
        }

        public override ISearchDomain GetDeclaredElementSearchDomain(IDeclaredElement declaredElement)
        {
            if (declaredElement is not IShaderLabDeclaredElement)
                return EmptySearchDomain.Instance;

            if (declaredElement is IPropertyDeclaredElement)
                return mySearchDomainFactory.CreateSearchDomain(declaredElement.GetSourceFiles());
            return mySearchDomainFactory.CreateSearchDomain(declaredElement.GetSolution(), false);
        }
    }
}