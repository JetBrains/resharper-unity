using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Impl;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Parsing
{ 
  internal class UnityYamlChameleonDocument : YamlDocument, IChameleonNode
  {

    
    private readonly object mySyncObject = new object();
    private bool myOpened;
    
    public UnityYamlChameleonDocument(ClosedChameleonElement closedChameleonElement)
    {
      AppendNewChild(closedChameleonElement);
    }

    public bool IsOpened
    {
      get
      {
        lock (mySyncObject)
          return myOpened;
      }
    }

    public override ITreeNode FirstChild
    {
      get
      {
        lock (mySyncObject)
        {
          if (!myOpened)
            OpenChameleon();
          return firstChild;
        }
      }
    }

    public override ITreeNode LastChild
    {
      get
      {
        lock (mySyncObject)
        {
          if (!myOpened)
            OpenChameleon();
          return lastChild;
        }
      }
    }

    public override int GetTextLength()
    {
      lock (mySyncObject)
        return base.GetTextLength();
    }

    public override StringBuilder GetText(StringBuilder to)
    {
      lock (mySyncObject)
        return base.GetText(to);
    }

    public override IBuffer GetTextAsBuffer()
    {
      lock (mySyncObject)
        return base.GetTextAsBuffer();
    }

    protected override TreeElement DeepClone(TreeNodeCopyContext context)
    {
      lock (mySyncObject)
        return base.DeepClone(context);
    }

    public IChameleonNode ReSync(CachingLexer cachingLexer, TreeTextRange changedRange, int insertedTextLen)
    {
      return null;
    }

    public override IChameleonNode FindChameleonWhichCoversRange(TreeTextRange textRange)
    {
      lock (mySyncObject)
      {
        if (textRange.ContainedIn(TreeTextRange.FromLength(GetTextLength())))
        {
          if (!myOpened)
            return this;

          return base.FindChameleonWhichCoversRange(textRange) ?? this;
        }
      }

      return null;
    }

    
    private void OpenChameleon()
    {
      Assertion.Assert(!myOpened, "!myOpened");
      AssertSingleChild();

      var service = PsiLanguageTypeExtensions.LanguageService(Language);
      Assertion.Assert(service != null, "service != null");

      var buffer = GetTextAsBuffer();
      var lexer = new TokenBuffer(service.GetPrimaryLexerFactory().CreateLexer(buffer)).CreateLexer();
      var parser = (YamlParser)service.CreateParser(lexer, null, GetSourceFile());
      var openedChameleon = parser.ParseDocument();

      AssertTextLength(openedChameleon);

      DeleteChildRange(firstChild, lastChild);
      OpenChameleonFrom(openedChameleon);
    }

    private void OpenChameleonFrom([NotNull] ITreeNode openedChameleon)
    {
      Assertion.Assert(firstChild == null && lastChild == null, "firstChild == null && lastChild == null");

      ITreeNode child;
      while ((child = openedChameleon.FirstChild) != null)
      {
        ((CompositeElement) openedChameleon).DeleteChildRange(child, child);
        AppendNewChild((TreeElement) child);
      }

      myOpened = true;
    }

    [Conditional("JET_MODE_ASSERT")]
    private void AssertSingleChild()
    {
      if (firstChild == lastChild && firstChild is IClosedChameleonBody)
        return;
      Assertion.Fail("One ChameleonElement child but found also {0}", lastChild.NodeType);
    }

    [Conditional("JET_MODE_ASSERT")]
    private void AssertTextLength(ITreeNode openedChameleon)
    {
      var expectedTextLength = firstChild.GetTextLength();
      var actualTextLength = openedChameleon.GetTextLength();
      Assertion.Assert(expectedTextLength == actualTextLength, "Chameleon length differ after opening! {0} {1}",
        expectedTextLength, actualTextLength);
    }

    public override string ToString() => "ChameleonDocument";
  }

}