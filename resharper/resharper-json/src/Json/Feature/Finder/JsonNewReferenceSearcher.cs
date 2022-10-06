using System.Collections.Generic;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Finder;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Json.Feature.Finder
{
    public class JsonNewReferenceSearcher : IDomainSpecificSearcher
    {
        private readonly IDeclaredElementsSet myElements;
        private readonly ReferenceSearcherParameters myReferenceSearcherParameters;
        private readonly List<string> myElementNames;

        public JsonNewReferenceSearcher(IDomainSpecificSearcherFactory searchWordsProvider,
            IDeclaredElementsSet elements, ReferenceSearcherParameters referenceSearcherParameters)
        {
            myElements = elements;
            myReferenceSearcherParameters = referenceSearcherParameters;

            myElementNames = new List<string>(elements.Count);
            foreach (var element in elements)
            {
                foreach (var name in searchWordsProvider.GetAllPossibleWordsInFile(element))
                    myElementNames.Add(name);
            }
        }

        public bool ProcessProjectItem<TResult>(IPsiSourceFile sourceFile, IFindResultConsumer<TResult> consumer)
        {
            if (sourceFile.GetPrimaryPsiFile() is IJsonNewFile jsonNewFile)
                return ProcessElement(jsonNewFile, consumer);
            return false;
        }

        public bool ProcessElement<TResult>(ITreeNode element, IFindResultConsumer<TResult> consumer)
        {
            Assertion.AssertNotNull(element, "element != null");
            var wordsInText = myElementNames;
            var referenceNames = myElementNames;
            var result = new ReferenceSearchSourceFileProcessor<TResult>(element, myReferenceSearcherParameters, consumer,
                myElements, wordsInText, referenceNames).Run();
            return result == FindExecution.Stop;
        }
    }
}