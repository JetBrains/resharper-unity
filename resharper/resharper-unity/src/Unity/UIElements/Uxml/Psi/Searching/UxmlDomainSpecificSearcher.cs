using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Finder;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.Searching
{
    public class UxmlDomainSpecificSearcher : IDomainSpecificSearcher
    {
        private readonly ReferenceSearcherParameters myReferenceSearcherParameters;
        private readonly OneToSetMap<IDeclaredElement, string> myElementNames = new();
        
        public UxmlDomainSpecificSearcher(UxmlScriptSearcherFactory searcher, IEnumerable<IDeclaredElement> elements, ReferenceSearcherParameters referenceSearcherParameters)
        {
            myReferenceSearcherParameters = referenceSearcherParameters;
            foreach (var element in elements)
            {
                myElementNames.Add(element, element.ShortName);
            }
        }
        
        public bool ProcessProjectItem<TResult>(IPsiSourceFile sourceFile, IFindResultConsumer<TResult> consumer)
        {
            return sourceFile.GetPsiFiles<UxmlLanguage>(myReferenceSearcherParameters.LanguageCategories).Any(psiFile => ProcessElement(psiFile, consumer));
        }

        public bool ProcessElement<TResult>(ITreeNode element, IFindResultConsumer<TResult> consumer)
        {
            foreach (var pair in myElementNames)
            {
                var findExecution = new ReferenceSearchSourceFileProcessor<TResult>(element, myReferenceSearcherParameters, consumer, new DeclaredElementsSet(pair.Key), null, pair.Value).Run();
                if (findExecution == FindExecution.Stop)
                    return true;
            }

            return false;
        }
    }
}