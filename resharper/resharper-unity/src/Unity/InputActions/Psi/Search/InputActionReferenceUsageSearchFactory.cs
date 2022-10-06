#nullable enable

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Finder;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Resx.Tree;
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
            var declaredElements = elements.FilterByType<InputActionsDeclaredElement>();
            if (declaredElements.IsEmpty()) return null;
            
            return new CSharpInputActionsReferenceSearcher(elements, declaredElements.SelectMany(GetAllPossibleWordsInFile).ToJetHashSet(), declaredElements.Select(element => element.ShortName).ToJetHashSet(), referenceSearcherParameters);
        }
    }
    
    internal class CSharpInputActionsReferenceSearcher : IDomainSpecificSearcher
  {
    private readonly IDeclaredElementsSet myElements;
    private readonly ICollection<string> myWordsInFiles;
    private readonly ICollection<string> myNames;
    private readonly ReferenceSearcherParameters myReferenceSearcherParameters;

    public CSharpInputActionsReferenceSearcher([NotNull] IDeclaredElementsSet elements, ICollection<string> wordsInFiles, ICollection<string> names, ReferenceSearcherParameters referenceSearcherParameters)
    {
      myElements = elements;
      myWordsInFiles = wordsInFiles;
      myNames = names;
      myReferenceSearcherParameters = referenceSearcherParameters;
    }

    public bool ProcessProjectItem<TResult>(IPsiSourceFile sourceFile, IFindResultConsumer<TResult> consumer)
    {
      if (!sourceFile.GetPsiServices().WordIndex.GetFilesContainingWords(myWordsInFiles).Contains(sourceFile))
        return false;

      return sourceFile.GetPsiFiles<CSharpLanguage>().Any(file => ProcessElement(file, consumer));
    }

    public bool ProcessElement<TResult>(ITreeNode element, IFindResultConsumer<TResult> consumer)
    {
      return new ResoureceReferenceSearchSourceFileProcessor<TResult>(element, myReferenceSearcherParameters, consumer, myElements, myWordsInFiles, myNames).Run() == FindExecution.Stop;
    }

    private class ResoureceReferenceSearchSourceFileProcessor<TResult> : ReferenceSearchSourceFileProcessor<TResult>
    {
      public ResoureceReferenceSearchSourceFileProcessor(ITreeNode treeNode, ReferenceSearcherParameters referenceSearcherParameters, IFindResultConsumer<TResult> resultConsumer, IDeclaredElementsSet elements, ICollection<string> wordsInText, ICollection<string> referenceNames) 
        : base(treeNode, referenceSearcherParameters, resultConsumer, elements, wordsInText, referenceNames)
      {
      }

      protected override bool PreFilterReference(IReference reference)
      {
        return reference is IResourceItemReference;
      }
    }
  }
    //
    // public class InputActionsReferenceSearcher : IDomainSpecificSearcher
    // {
    //     private readonly IDeclaredElementsSet myElements;
    //     private readonly List<string> myElementNames;
    //     private readonly List<string> myElementGuids;
    //     private readonly ReferenceSearcherParameters myReferenceSearcherParameters;
    //
    //     public InputActionsReferenceSearcher(IDeclaredElementsSet elements, List<string> guids, ReferenceSearcherParameters referenceSearcherParameters)
    //     {
    //         myElements = elements;
    //         myElementGuids = guids;
    //         myReferenceSearcherParameters = referenceSearcherParameters;
    //
    //         myElementNames = new List<string>();
    //         foreach (var element in elements)
    //             myElementNames.Add(element.ShortName);
    //     }
    //
    //     public bool ProcessProjectItem<TResult>(IPsiSourceFile sourceFile, IFindResultConsumer<TResult> consumer)
    //     {
    //         if ((!sourceFile.IsInputActions())
    //             || sourceFile.GetPrimaryPsiFile() is not IJsonNewFile jsonNewFile)
    //         {
    //             return false;
    //         }
    //         return ProcessElement(jsonNewFile, consumer);
    //     }
    //
    //     public bool ProcessElement<TResult>(ITreeNode element, IFindResultConsumer<TResult> consumer)
    //     {
    //         var result = new ReferenceSearchSourceFileProcessor<TResult>(element, myReferenceSearcherParameters, consumer,
    //             myElements, myElementNames, myElementGuids).Run();
    //         return result == FindExecution.Stop;
    //     }
    // }
}