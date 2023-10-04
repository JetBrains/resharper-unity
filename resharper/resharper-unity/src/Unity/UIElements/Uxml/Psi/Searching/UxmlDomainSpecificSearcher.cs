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
        private readonly OneToSetMap<IDeclaredElement, string> myElementWordsInText = new();
        
        public UxmlDomainSpecificSearcher(UxmlScriptSearcherFactory searcher, IEnumerable<IDeclaredElement> elements, ReferenceSearcherParameters referenceSearcherParameters)
        {
            myReferenceSearcherParameters = referenceSearcherParameters;
            foreach (var element in elements)
            {
                myElementNames.Add(element, element.ShortName);

                if (string.IsNullOrEmpty(element.ShortName))
                    myElementWordsInText.AddRange(element, EmptyList<string>.InstanceList);

                myElementWordsInText.AddRange(element, searcher.GetAllPossibleWordsInFile(element));
            }
        }
        
        public bool ProcessProjectItem<TResult>(IPsiSourceFile sourceFile, IFindResultConsumer<TResult> consumer)
        {
            return sourceFile.GetPsiFiles<UxmlLanguage>().Any(psiFile => ProcessElement(psiFile, consumer));
        }

        public bool ProcessElement<TResult>(ITreeNode element, IFindResultConsumer<TResult> consumer)
        {
            foreach (var pair in myElementNames)
            {
                var findExecution = new ReferenceSearchSourceFileProcessor<TResult>(element, myReferenceSearcherParameters, consumer, new DeclaredElementsSet(pair.Key), myElementWordsInText[pair.Key], pair.Value).Run();
                if (findExecution == FindExecution.Stop)
                    return true;
            }

            return false;
        }
    }
}