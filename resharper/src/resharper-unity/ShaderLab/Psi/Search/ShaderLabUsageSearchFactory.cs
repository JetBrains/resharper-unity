using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Impl.Search.SearchDomain;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Search
{
    [PsiSharedComponent]
    public class ShaderLabUsageSearchFactory : IDomainSpecificSearcherFactory
    {
        private readonly SearchDomainFactory mySearchDomainFactory;

        public ShaderLabUsageSearchFactory(SearchDomainFactory searchDomainFactory)
        {
            mySearchDomainFactory = searchDomainFactory;
        }

        public bool IsCompatibleWithLanguage(PsiLanguageType languageType)
        {
            return languageType.Is<ShaderLabLanguage>();
        }

        public IDomainSpecificSearcher CreateConstructorSpecialReferenceSearcher(ICollection<IConstructor> constructors)
        {
            return null;
        }

        public IDomainSpecificSearcher CreateMethodsReferencedByDelegateSearcher(IDelegate @delegate)
        {
            return null;
        }

        public IDomainSpecificSearcher CreateReferenceSearcher(IDeclaredElementsSet elements, bool findCandidates)
        {
            if (elements.Any(element => !(element is IShaderLabDeclaredElement)))
                return null;
            return new ShaderLabReferenceSearcher(elements, findCandidates);
        }

        public IDomainSpecificSearcher CreateLateBoundReferenceSearcher(IDeclaredElementsSet elements)
        {
            return null;
        }

        public IDomainSpecificSearcher CreateTextOccurrenceSearcher(IDeclaredElementsSet elements)
        {
            return new ShaderLabTextOccurrenceSearcher(elements);
        }

        public IDomainSpecificSearcher CreateTextOccurrenceSearcher(string subject)
        {
            return new ShaderLabTextOccurrenceSearcher(subject);
        }

        public IDomainSpecificSearcher CreateAnonymousTypeSearcher(IList<AnonymousTypeDescriptor> typeDescription, bool caseSensitive)
        {
            return null;
        }

        public IDomainSpecificSearcher CreateConstantExpressionSearcher(ConstantValue constantValue, bool onlyLiteralExpression)
        {
            return null;
        }

        public IEnumerable<string> GetAllPossibleWordsInFile(IDeclaredElement element)
        {
            yield return element.ShortName;
        }

        public IEnumerable<RelatedDeclaredElement> GetRelatedDeclaredElements(IDeclaredElement element)
        {
            return EmptyList<RelatedDeclaredElement>.InstanceList;
        }

        public Tuple<ICollection<IDeclaredElement>, Predicate<IFindResultReference>, bool> GetDerivedFindRequest(IFindResultReference result)
        {
            return null;
        }

        public Tuple<ICollection<IDeclaredElement>, bool> GetNavigateToTargets(IDeclaredElement element)
        {
            return null;
        }

        public ICollection<FindResult> TransformNavigationTargets(ICollection<FindResult> targets)
        {
            return targets;
        }

        public ISearchDomain GetDeclaredElementSearchDomain(IDeclaredElement declaredElement)
        {
            if (!(declaredElement is IShaderLabDeclaredElement))
                return EmptySearchDomain.Instance;

            return mySearchDomainFactory.CreateSearchDomain(declaredElement.GetSourceFiles());
        }
    }
}