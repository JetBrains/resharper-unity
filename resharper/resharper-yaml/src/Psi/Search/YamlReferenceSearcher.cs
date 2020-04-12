using System;
using System.Collections.Generic;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Search
{
  public class YamlReferenceSearcher : IDomainSpecificSearcher
  {
    private readonly IDeclaredElementsSet myElements;
    private readonly bool myFindCandidates;
    protected readonly List<string> ElementNames;

    public YamlReferenceSearcher(IDomainSpecificSearcherFactory searchWordsProvider, IDeclaredElementsSet elements,
      bool findCandidates)
    {
      myElements = elements;
      myFindCandidates = findCandidates;

      ElementNames = new List<string>(elements.Count);
      foreach (var element in elements)
      {
        foreach (var name in searchWordsProvider.GetAllPossibleWordsInFile(element))
          ElementNames.Add(name);
      }
    }

    // Note that some searchers (such as ReferenceSearchProcessorBase) will filter by word index before calling this.
    // Words come from IDomainSpecificSearcherFactory.GetAllPossibleWordsInFile
    public bool ProcessProjectItem<TResult>(IPsiSourceFile sourceFile, IFindResultConsumer<TResult> consumer)
    {
      if (sourceFile.GetPrimaryPsiFile() is IYamlFile yamlFile)
        return ProcessElement(yamlFile, consumer);
      return false;
    }

    public bool ProcessElement<TResult>(ITreeNode element, IFindResultConsumer<TResult> consumer)
    {
      Assertion.AssertNotNull(element, "element != null");
      // wordsInText is used to create string searchers, which are used to see if chameleon subtree should be opened.
      // If this is null or empty, then all references are processed, without skipping chameleons. References are cached
      // in both cases.
      // referenceNames is used to create a reference name container which is used to optimise things. It's passed to
      // the reference provider's HasReferences to get a false or "maybe" based on name. It's then used (along with
      // PreFilterReference) to filter references before they're resolved, based on GetName/GetAllNames.
      // Normally, wordsInText will match referenceNames, as the reference's GetName will return a string that is also
      // in the text. One example of a reference with a different name is a constructor initialiser, where the name is
      // .ctor, but would appear in text as this or base
      var wordsInText = ElementNames;
      var referenceNames = ElementNames;
      var result = new ReferenceSearchSourceFileProcessorWorkaround<TResult>(element, myFindCandidates, consumer, myElements,
        wordsInText, referenceNames).Run();
      return result == FindExecution.Stop;
    }
  }
}
