using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.Util;

// This file is required because NamedThingsSearchSourceFileProcessor uses ThisAndDescendantsEnumerator which has a bug.
// If we set EnumerateNextSibling on the last node in a sub-tree, we never reset the state to EnumerateDescendants and
// skip all remaining sub-trees.

// This file should be deleted as soon as this is fixed in the SDK

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Search
{
  internal class ReferenceSearchSourceFileProcessorWorkaround<TResult> : NamedThingsSearchSourceFileProcessorWorkaround
  {
    private readonly bool myFindCandidates;
    private readonly IFindResultConsumer<TResult> myResultConsumer;

    public ReferenceSearchSourceFileProcessorWorkaround(ITreeNode treeNode, bool findCandidates,
      IFindResultConsumer<TResult> resultConsumer, IDeclaredElementsSet elements,
      ICollection<string> wordsInText, ICollection<string> referenceNames)
      : base(treeNode, wordsInText, referenceNames, elements)
    {
      myFindCandidates = findCandidates;
      myResultConsumer = resultConsumer;
    }

    public IFindResultConsumer<TResult> ResultConsumer
    {
      get { return myResultConsumer; }
    }

    protected override bool PreFilterReference(IReference reference)
    {
      return true;
    }

    protected virtual IResolveResult Resolve(IReference reference)
    {
      return reference.Resolve().Result;
    }

    protected virtual bool AcceptElement(IDeclaredElement resolvedElement)
    {
      return Elements.Contains(resolvedElement);
    }

    protected override FindExecution ProcessReference(IReference reference)
    {
      if (myFindCandidates)
      {
        foreach (var element in Resolve(reference).Elements())
        {
          var resolved = element.Element;
          if (AcceptElement(resolved))
          {
            return ResultConsumer.Accept(new FindResultReference(reference, resolved));
          }
        }
      }
      else
      {
        var resolved = Resolve(reference).DeclaredElement;
        if (resolved != null && AcceptElement(resolved))
        {
          return ResultConsumer.Accept(new FindResultReference(reference, resolved));
        }
      }

      return FindExecution.Continue;
    }
  }

  internal abstract class NamedThingsSearchSourceFileProcessorWorkaround
  {
    private readonly ITreeNode myRoot;

    private readonly IReferenceNameContainer myReferenceNameContainer;
    private readonly StringSearcher[] myStringSearchers;
    private readonly IDeclaredElementsSet myElements;

    private readonly bool myCaseSensitive;
    private readonly IReferenceProvider myReferenceProvider;

    protected NamedThingsSearchSourceFileProcessorWorkaround(ITreeNode root, ICollection<string> wordsInText,
      ICollection<string> referenceNames, IDeclaredElementsSet elements)
    {
      myRoot = root;
      myReferenceProvider = GetReferenceProvider2((TreeElement) root);
      myElements = elements;

      var languageService = root.Language.LanguageService();
      if (languageService != null) myCaseSensitive = languageService.IsCaseSensitive;
      myCaseSensitive = myCaseSensitive && elements.CaseSensitive;

      if (wordsInText != null && wordsInText.Count > 0)
        myStringSearchers = wordsInText.Where(word => !string.IsNullOrEmpty(word))
          .Select(word => new StringSearcher(word, myCaseSensitive)).ToArray();

      if (referenceNames != null && referenceNames.Count > 0)
        myReferenceNameContainer = new ReferenceNameContainer(referenceNames, myCaseSensitive);
    }

    internal IReferenceProvider GetReferenceProvider2(TreeElement root)
    {
      for (TreeElement treeElement = root; treeElement != null; treeElement = (TreeElement) treeElement.parent)
      {
        switch (treeElement)
        {
          case IFileImpl fileImpl:
            IReferenceProvider referenceProvider = fileImpl.ReferenceProvider;
            if (referenceProvider != null)
              return referenceProvider;
            break;
          case ISandBox sandBox:
            ITreeNode contextNode = sandBox.ContextNode;
            if (contextNode != null)
              return GetReferenceProvider2((TreeElement) contextNode);
            break;
        }
      }

      return null;
    }

    public FindExecution Run()
    {
      var scope = myRoot.Root();
      if (myStringSearchers == null)
        return RunForAllReferences(scope);
      return RunForNamedReferences(scope);
    }

    private FindExecution RunForNamedReferences(ITreeNode scope)
    {
      var suspiciousRefs = new List<IReference>();

      var interruptChecker = new SeldomInterruptChecker();

      for (var descendants = new FixedThisAndDescendantsEnumerator(myRoot); descendants.MoveNext();)
      {
        var element = descendants.Current;

        foreach (var reference in GetReferences(element))
        {
          suspiciousRefs.Add(reference);
        }

        if (!ShouldVisitScope(element))
        {
          descendants.SkipThisNode();
        }

        interruptChecker.CheckForInterrupt();
      }

      if (suspiciousRefs.Count > 0)
      {
        MultipleReferencesResolver.ResolveReferences(scope, suspiciousRefs);
      }

      foreach (var reference in suspiciousRefs)
      {
        var execution = ProcessReference(reference);
        if (execution == FindExecution.Stop)
          return FindExecution.Stop;
        interruptChecker.CheckForInterrupt();
      }

      return FindExecution.Continue;
    }

    private FindExecution RunForAllReferences(ITreeNode scope)
    {
      new CachingNonQualifiedReferencesResolver().Process(scope);

      foreach (var element in new FixedThisAndDescendantsEnumerator(myRoot))
      {
        foreach (var reference in GetReferences(element))
        {
          if (PreFilterReference(reference) && ReferenceNamePredicate(reference))
          {
            var execution = ProcessReference(reference);
            if (execution == FindExecution.Stop)
              return FindExecution.Stop;
          }
        }
      }

      return FindExecution.Continue;
    }

    protected IDeclaredElementsSet Elements
    {
      get { return myElements; }
    }

    protected abstract FindExecution ProcessReference(IReference reference);
    protected abstract bool PreFilterReference(IReference reference);

    private bool ReferenceNamePredicate(IReference reference)
    {
      if (myReferenceNameContainer == null)
        return true;

      if (reference.HasMultipleNames)
      {
        foreach (var name in reference.GetAllNames())
        {
          if (myReferenceNameContainer.Contains(name))
            return true;
        }

        return false;
      }

      return myReferenceNameContainer.Contains(reference.GetName());
    }

    protected virtual bool ShouldVisitScope(ITreeNode element)
    {
      var chameleonNode = element as IChameleonNode;
      if (chameleonNode == null || chameleonNode.IsOpened) return true;

      return SubTreeContainsText(chameleonNode);
    }

    private static readonly Func<NamedThingsSearchSourceFileProcessorWorkaround, IReference, bool> ourFilter =
      (processor, reference) => processor.PreFilterReference(reference) && processor.ReferenceNamePredicate(reference);

    protected virtual ReferenceCollection GetReferences(ITreeNode element)
    {
      return element.GetReferences(myReferenceProvider, myReferenceNameContainer).Where(this, ourFilter);
    }

    protected virtual bool SubTreeContainsText(ITreeNode node)
    {
      return BufferContainsText(node.GetTextAsBuffer());
    }

    protected bool BufferContainsText(IBuffer buffer)
    {
      if (myStringSearchers == null)
        return true;

      foreach (var stringSearcher in myStringSearchers)
      {
        if (stringSearcher.Find(buffer) >= 0)
          return true;
      }

      return false;
    }
  }

  [StructLayout(LayoutKind.Auto), EditorBrowsable(EditorBrowsableState.Never)]
  internal struct FixedThisAndDescendantsEnumerator
  {
    [CanBeNull] internal readonly TreeElement myRoot;
    [CanBeNull] private TreeElement myCurrent;
    private EnumerationState myState;

    private enum EnumerationState
    {
      EnumerateThis,
      EnumerateDescendants,
      EnumerateNextSibling,
      StopNextTime
    }

    internal FixedThisAndDescendantsEnumerator([CanBeNull] ITreeNode root)
    {
      myRoot = (TreeElement) root;
      myCurrent = (TreeElement) root;
      myState = EnumerationState.EnumerateThis;
    }

    [Pure]
    public FixedThisAndDescendantsEnumerator GetEnumerator() => this;

    // note: DO NOT implement this method to avoid wrapping
    //       foreach loops into try-finally by C# compiler:
    // public void Dispose()

    public ITreeNode Current => myCurrent;

    public bool MoveNext()
    {
      var treeNode = myCurrent;
      if (treeNode == null) return false;

      switch (myState)
      {
        case EnumerationState.EnumerateThis:
          myState = (treeNode.FirstChild != null)
            ? EnumerationState.EnumerateDescendants
            : EnumerationState.StopNextTime;
          return true;

        case EnumerationState.EnumerateDescendants:
          var firstChild = (TreeElement) treeNode.FirstChild;
          if (firstChild != null)
          {
            myCurrent = firstChild;
            return true;
          }

          goto case EnumerationState.EnumerateNextSibling;

        case EnumerationState.EnumerateNextSibling:
          var nextSibling = treeNode.nextSibling;
          if (nextSibling != null)
          {
            myCurrent = nextSibling;
            myState = EnumerationState.EnumerateDescendants;
            return true;
          }

          break;

        case EnumerationState.StopNextTime:
          return false;
      }

      while (true)
      {
        myCurrent = treeNode.parent;
        treeNode = myCurrent;

        if (treeNode == myRoot | treeNode == null) // yes, |
          return false;

        var nextSibling = treeNode.nextSibling;
        if (nextSibling != null)
        {
          myCurrent = nextSibling;
          // FIX: This line is missing in the SDK version
          myState = EnumerationState.EnumerateDescendants;
          return true;
        }
      }
    }

    public void SkipThisNode()
    {
      switch (myState)
      {
        case EnumerationState.EnumerateThis:
          myState = EnumerationState.StopNextTime;
          break;

        case EnumerationState.EnumerateDescendants:
          myState = myCurrent == myRoot
            ? EnumerationState.StopNextTime
            : EnumerationState.EnumerateNextSibling;
          break;
      }
    }
  }
}