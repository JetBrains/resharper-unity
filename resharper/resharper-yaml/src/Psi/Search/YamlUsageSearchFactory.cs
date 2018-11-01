using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Impl.Search.SearchDomain;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Search
{
  [PsiSharedComponent]
  public class YamlUsageSearchFactory : IDomainSpecificSearcherFactory
  {
    public bool IsCompatibleWithLanguage(PsiLanguageType languageType)
    {
      return languageType.Is<YamlLanguage>();
    }

    public IDomainSpecificSearcher CreateReferenceSearcher(IDeclaredElementsSet elements, bool findCandidates)
    {
      if (elements.All(e => e is IMethod))
        return new YamlReferenceSearcher(elements, findCandidates);
      return null;
    }

    // Used to filter files before searching for references
    public IEnumerable<string> GetAllPossibleWordsInFile(IDeclaredElement element) => new[] {element.ShortName};

    public IDomainSpecificSearcher CreateConstructorSpecialReferenceSearcher(ICollection<IConstructor> constructors) => null;
    public IDomainSpecificSearcher CreateLateBoundReferenceSearcher(IDeclaredElementsSet elements) => null;
    public IDomainSpecificSearcher CreateMethodsReferencedByDelegateSearcher(IDelegate @delegate) => null;
    public IDomainSpecificSearcher CreateAnonymousTypeSearcher(IList<AnonymousTypeDescriptor> typeDescription, bool caseSensitive) => null;
    public IDomainSpecificSearcher CreateConstantExpressionSearcher(ConstantValue constantValue, bool onlyLiteralExpression) => null;
    public IEnumerable<RelatedDeclaredElement> GetRelatedDeclaredElements(IDeclaredElement element) => EmptyList<RelatedDeclaredElement>.Enumerable;
    public Tuple<ICollection<IDeclaredElement>, Predicate<IFindResultReference>, bool> GetDerivedFindRequest( IFindResultReference result) => null;
    public Tuple<ICollection<IDeclaredElement>, bool> GetNavigateToTargets(IDeclaredElement element) => null;
    public ICollection<FindResult> TransformNavigationTargets(ICollection<FindResult> targets) => targets;
    public ISearchDomain GetDeclaredElementSearchDomain(IDeclaredElement declaredElement) => EmptySearchDomain.Instance;

    // TODO: Perhaps support discovering e.g. block scalar text at some point in the future?
    public IDomainSpecificSearcher CreateTextOccurrenceSearcher(IDeclaredElementsSet elements) => null;
    public IDomainSpecificSearcher CreateTextOccurrenceSearcher(string subject) => null;
  }
}