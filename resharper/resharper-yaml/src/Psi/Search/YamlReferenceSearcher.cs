using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Finder;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Search
{
  public class YamlReferenceSearcher : IDomainSpecificSearcher
  {
    private readonly IDeclaredElementsSet myElements;
    private readonly bool myFindCandidates;
    private readonly List<string> myElementNames;

    public YamlReferenceSearcher(IDeclaredElementsSet elements, bool findCandidates)
    {
      myElements = elements;
      myFindCandidates = findCandidates;

      myElementNames = new List<string>(elements.Count);
      foreach (var element in elements)
        myElementNames.Add(element.ShortName);
    }

    public bool ProcessProjectItem<TResult>(IPsiSourceFile sourceFile, IFindResultConsumer<TResult> consumer)
    {
      if (sourceFile.GetPrimaryPsiFile() is IYamlFile yamlFile)
        return ProcessElement(yamlFile, consumer);
      return false;
    }

    public bool ProcessElement<TResult>(ITreeNode element, IFindResultConsumer<TResult> consumer)
    {
      Assertion.AssertNotNull(element, "element != null");
      var result = new ReferenceSearchSourceFileProcessor<TResult>(element, myFindCandidates, consumer, myElements,
        myElementNames, myElementNames).Run();
      return result == FindExecution.Stop;
    }
  }
}