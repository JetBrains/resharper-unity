using System.Collections.Generic;
using System.Linq;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.InputActions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Impl.Search.SearchDomain;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Search
{
    // from csharp to inputactions
    [PsiSharedComponent]
    public class UnityInputActionsReferenceUsageSearchFactory : IDomainSpecificSearcherFactory
    {
        public bool IsCompatibleWithLanguage(PsiLanguageType languageType)
        {
            return languageType.Is<JsonNewLanguage>(); //languageType.Is<CSharpLanguage>();
        }

        public IDomainSpecificSearcher CreateConstructorSpecialReferenceSearcher(ICollection<IConstructor> constructors)
        {
            return null;
        }

        public IDomainSpecificSearcher CreateTargetTypedObjectCreationSearcher(IReadOnlyList<IConstructor> constructors, IReadOnlyList<ITypeElement> typeElements,
            ReferenceSearcherParameters referenceSearcherParameters)
        {
            return null;
        }

        public IDomainSpecificSearcher CreateMethodsReferencedByDelegateSearcher(IDelegate @delegate)
        {
            return null;
        }

        public IDomainSpecificSearcher CreateReferenceSearcher(IDeclaredElementsSet elements,
            ReferenceSearcherParameters referenceSearcherParameters)
        {
            var declaredElements = new DeclaredElementsSet(elements.Where(IsInterestingElement));
            
            if (!declaredElements.Any())
                return null;
        
            var solution = elements.First().GetSolution();
            var container = solution.GetComponent<InputActionsElementContainer>();

            return new CSharpInputActionsReferenceSearcher(declaredElements, container);
        }

        public IDomainSpecificSearcher CreateLateBoundReferenceSearcher(IDeclaredElementsSet elements,
            ReferenceSearcherParameters referenceSearcherParameters)
        {
            return null;
        }

        public IDomainSpecificSearcher CreateTextOccurrenceSearcher(IDeclaredElementsSet elements)
        {
            return null;
        }

        public IDomainSpecificSearcher CreateTextOccurrenceSearcher(string subject)
        {
            return null;
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
            if (IsInterestingElement(element))
                yield return element.ShortName.Substring(2);
        }

        public IEnumerable<RelatedDeclaredElement> GetRelatedDeclaredElements(IDeclaredElement element)
        {
            return EmptyList<RelatedDeclaredElement>.ReadOnly;
        }

        public IEnumerable<FindResult> GetRelatedFindResults(IDeclaredElement element)
        {
            return EmptyList<FindResult>.ReadOnly;
        }

        public DerivedFindRequest GetDerivedFindRequest(IFindResultReference result)
        {
            return null;
        }

        public NavigateTargets GetNavigateToTargets(IDeclaredElement element)
        {
           return NavigateTargets.Empty;
        }

        public ICollection<FindResult> TransformNavigationTargets(ICollection<FindResult> targets)
        {
            return null;
        }

        public ISearchDomain GetDeclaredElementSearchDomain(IDeclaredElement declaredElement)
        {
            return EmptySearchDomain.Instance;
        }
        
        public static bool IsInterestingElement(IDeclaredElement element)
        {
            if (element is IMethod { IsStatic: false } method)
            {
                if (method.ShortName.StartsWith("On"))
                {
                    return method.ContainingType is IClass classType && classType.DerivesFromMonoBehaviour();
                }
                return false;
            }

            return false;
        }
    }
}