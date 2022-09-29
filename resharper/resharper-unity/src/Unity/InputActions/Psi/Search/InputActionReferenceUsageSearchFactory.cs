#nullable enable

using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.DeclaredElements;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Finder;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.Search
{
    [PsiSharedComponent]
    public class InputActionReferenceUsageSearchFactory : DomainSpecificSearcherFactoryBase
    {
        public override bool IsCompatibleWithLanguage(PsiLanguageType languageType) =>
            languageType.Is<JsonNewLanguage>();

        public override IEnumerable<string> GetAllPossibleWordsInFile(IDeclaredElement element)
        {
            if (IsInterestingElement(element))
            {
                return new [] {element.ShortName};
            }

            return EmptyList<string>.Instance;
        }
        
        private static bool IsInterestingElement(IDeclaredElement element)
        {
            return element is InputActionsDeclaredElement;
        }

        public override IDomainSpecificSearcher? CreateReferenceSearcher(
            IDeclaredElementsSet elements, ReferenceSearcherParameters referenceSearcherParameters)
        {
            if (elements.Any(e => e is not InputActionsDeclaredElement))
                return null;

            var solution = elements.First().GetSolution();
            var cache = solution.GetComponent<InputActionsCache>();

            var list = new List<string>();
            foreach (var element in elements)
            {
                var item = cache.GetNames(element);
                list.AddRange(item);
            }
            
            // todo: we should be looking for usages in csharp files

            return new InputActionsReferenceSearcher(elements, list, referenceSearcherParameters);
        }
    }

    public class InputActionsReferenceSearcher : IDomainSpecificSearcher
    {
        private readonly IDeclaredElementsSet myElements;
        private readonly List<string> myElementNames;
        private readonly List<string> myElementGuids;
        private readonly ReferenceSearcherParameters myReferenceSearcherParameters;

        public InputActionsReferenceSearcher(IDeclaredElementsSet elements, List<string> guids, ReferenceSearcherParameters referenceSearcherParameters)
        {
            myElements = elements;
            myElementGuids = guids;
            myReferenceSearcherParameters = referenceSearcherParameters;

            myElementNames = new List<string>();
            foreach (var element in elements)
                myElementNames.Add(element.ShortName);
        }

        public bool ProcessProjectItem<TResult>(IPsiSourceFile sourceFile, IFindResultConsumer<TResult> consumer)
        {
            if ((!sourceFile.IsInputActions())
                || sourceFile.GetPrimaryPsiFile() is not IJsonNewFile jsonNewFile)
            {
                return false;
            }
            return ProcessElement(jsonNewFile, consumer);
        }

        public bool ProcessElement<TResult>(ITreeNode element, IFindResultConsumer<TResult> consumer)
        {
            var result = new ReferenceSearchSourceFileProcessor<TResult>(element, myReferenceSearcherParameters, consumer,
                myElements, myElementNames, myElementGuids).Run();
            return result == FindExecution.Stop;
        }
    }
}