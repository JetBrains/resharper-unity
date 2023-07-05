using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Impl.Search.SearchDomain;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Search
{
    [PsiSharedComponent]
    public class UnityDotsSearchFactory : IDomainSpecificSearcherFactory
    {
        private readonly IContextBoundSettingsStore mySettingsStore;

        public UnityDotsSearchFactory(Lifetime lifetime, ISettingsStore settingsStore)
        {
            mySettingsStore = settingsStore.BindToContextLive(lifetime, ContextRange.ApplicationWide);
        }
        public bool IsCompatibleWithLanguage(PsiLanguageType languageType)
        {
            return languageType.Is<CSharpLanguage>();
        }

        public IDomainSpecificSearcher CreateConstructorSpecialReferenceSearcher(ICollection<IConstructor> constructors)
        {
            return null;
        }

        public IDomainSpecificSearcher CreateTargetTypedObjectCreationSearcher(IReadOnlyList<IConstructor> constructors,
            IReadOnlyList<ITypeElement> typeElements,
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
            return null;
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

        public IDomainSpecificSearcher CreateAnonymousTypeSearcher(IList<AnonymousTypeDescriptor> typeDescription,
            bool caseSensitive)
        {
            return null;
        }

        public IDomainSpecificSearcher CreateConstantExpressionSearcher(ConstantValue constantValue,
            bool onlyLiteralExpression)
        {
            return null;
        }

        public IEnumerable<string> GetAllPossibleWordsInFile(IDeclaredElement element)
        {
            return EmptyList<string>.ReadOnly;
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
            var hideGeneratedCode =
                mySettingsStore.GetValue((UnitySettings s) => s.HideGeneratedCodeFromNavigation);
            
            if (!hideGeneratedCode)
                return null;
            
            foreach (var result in targets)
            {
                if (IsDotsRelatedCodeGeneratedDeclaration(result))
                    return targets.Where(t => !IsDotsRelatedCodeGeneratedDeclaration(t)).ToList();
            }

            return null;
        }

        private static bool IsDotsRelatedCodeGeneratedDeclaration(FindResult result)
        {
            if (result is not FindResultDeclaration { Declaration: IClassLikeDeclaration {IsPartial: true } classLikeDeclaration } resultDeclaration)
                return false;
            
            if (!classLikeDeclaration.DeclaredElement.IsDotsImplicitlyUsedType())
                return false;

            return resultDeclaration.SourceFile.IsSourceGeneratedFile();
        }

        public ISearchDomain GetDeclaredElementSearchDomain(IDeclaredElement declaredElement)
        {
            return EmptySearchDomain.Instance;
        }
    }
}