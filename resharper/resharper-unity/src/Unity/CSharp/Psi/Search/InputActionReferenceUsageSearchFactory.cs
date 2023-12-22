using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.InputActions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Impl.Search.SearchDomain;
using JetBrains.ReSharper.Psi.Search;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Search
{
    // from csharp to inputactions
    [PsiSharedComponent]
    public class UnityInputActionsReferenceUsageSearchFactory : DomainSpecificSearcherFactoryBase
    {
        private readonly SearchDomainFactory mySearchDomainFactory;

        public UnityInputActionsReferenceUsageSearchFactory(SearchDomainFactory searchDomainFactory)
        {
            mySearchDomainFactory = searchDomainFactory;
        }
        public override bool IsCompatibleWithLanguage(PsiLanguageType languageType)
        {
            return languageType.Is<JsonNewLanguage>(); //languageType.Is<CSharpLanguage>();
        }

        public override IDomainSpecificSearcher CreateReferenceSearcher(IDeclaredElementsSet elements,
            ReferenceSearcherParameters referenceSearcherParameters)
        {
            var declaredElements = new DeclaredElementsSet(elements.Where(IsInterestingElement));
            
            if (!declaredElements.Any())
                return null;
        
            var solution = elements.First().GetSolution();
            var container = solution.GetComponent<InputActionsElementContainer>();

            return new CSharpInputActionsReferenceSearcher(declaredElements, container);
        }

        public override IEnumerable<string> GetAllPossibleWordsInFile(IDeclaredElement element)
        {
            if (IsInterestingElement(element))
                yield return element.ShortName.Substring(2);
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
            if (element is IMethod { IsStatic: false } method)
            {
                var shortName = method.ShortName;
                if (!shortName.StartsWith("On") || shortName.Length <= 2) return false;

                var cache = element.GetSolution().GetComponent<InputActionsCache>();
                return method.ContainingType is IClass classType 
                       && classType.DerivesFromMonoBehaviour() 
                       && cache.ContainsName(shortName[2..]);
            }

            return false;
        }
    }
}