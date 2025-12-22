using System.Diagnostics;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Impl;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.Impl
{
  internal class ChameleonDocumentBody : DocumentBody, IChameleonDocumentBody
  {
    private readonly object mySyncObject = new object();
    private bool myOpened;

    // Used (indirectly) by YamlTreeStructureBuilder to create an instance. It then calls AppendNewChild with a new
    // instance of ClosedChameleonElement
    public ChameleonDocumentBody()
    {
    }

    // Used by Resync to create a closed chameleon
    private ChameleonDocumentBody(ClosedChameleonElement closedChameleonElement)
    {
      AppendNewChild(closedChameleonElement);
    }

    // Used by Resync to create an open chameleon
    private ChameleonDocumentBody(IDocumentBody openedChameleon)
    {
      OpenChameleonFrom(openedChameleon);
    }

    public override NodeType NodeType => YamlChameleonElementTypes.CHAMELEON_DOCUMENT_BODY;

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
      var currentStartOffset = GetTreeStartOffset();
      var currentLength = GetTextLength();

      Assertion.Assert(
        changedRange.StartOffset >= currentStartOffset && changedRange.EndOffset <= currentStartOffset + currentLength,
        "changedRange.StartOffset >= currentStartOffset && changedRange.EndOffset <= (currentStartOffset+currentLength)");

      var newLength = currentLength - changedRange.Length + insertedTextLen;

      // Can we resync from the start point?
      if (!cachingLexer.FindTokenAt(currentStartOffset.Offset)
          || cachingLexer.TokenStart != currentStartOffset.Offset
          || !IsAtValidStartToken(cachingLexer))
      {
        return null;
      }

      // Try to find a valid end point
      TokenNodeType tokenType;
      var calculatedNewLength = 0;
      while ((tokenType = cachingLexer.TokenType) != null &&
             (calculatedNewLength += cachingLexer.TokenEnd - cachingLexer.TokenStart) < newLength)
      {
        // We shouldn't encounter these until the end of the changed range
        if (tokenType == YamlTokenType.DOCUMENT_END || tokenType == YamlTokenType.DIRECTIVES_END)
          return null;

        cachingLexer.Advance();
      }

      if (calculatedNewLength != newLength || !IsAtValidEndToken(cachingLexer))
        return null;

      // TODO: Should this be synchronised?
      // The C# implementation isn't...
      if (!myOpened)
      {
        var buffer = ProjectedBuffer.Create(cachingLexer.Buffer,
          new TextRange(currentStartOffset.Offset, currentStartOffset.Offset + newLength));
        var closedChameleon = new ClosedChameleonElement(YamlTokenType.CHAMELEON, buffer, TreeOffset.Zero, buffer.Length);
        return new ChameleonDocumentBody(closedChameleon);
      }

      var projectedLexer = new ProjectedLexer(cachingLexer, new TextRange(currentStartOffset.Offset, currentStartOffset.Offset + newLength));
      var parser =
        (IYamlParser) Language.LanguageService().CreateParser(projectedLexer, GetPsiModule(), GetSourceFile());

      var openedChameleon = parser.ParseDocumentBody();
      return new ChameleonDocumentBody(openedChameleon);
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

    private bool IsAtValidStartToken(CachingLexer cachingLexer)
    {
      // Make sure the current token is one we can start resyncing from. A body node can start with practically anything
      // so the best we can do is make sure there are no significant tokens before us - i.e. the only preceding tokens
      // should be whitespace, new lines, comment and document or directive end
      var tokenBuffer = cachingLexer.TokenBuffer;
      for (var i = cachingLexer.CurrentPosition - 1; i >= 0; i--)
      {
        var token = tokenBuffer[i];

        // Start of buffer, start of document, end of previous document
        if (token.Type == null || token.Type == YamlTokenType.DIRECTIVES_END || token.Type == YamlTokenType.DOCUMENT_END)
          return true;

        if (!token.Type.IsComment && !token.Type.IsWhitespace)
          return false;
      }

      return true;
    }

    private bool IsAtValidEndToken(CachingLexer cachingLexer)
    {
      // There is no token marking the exact end of the chameleon body - make sure the next significant token is EOF,
      // directives end or document end
      TokenNodeType tokenType;
      while ((tokenType = cachingLexer.TokenType) != null)
      {
        if (tokenType == YamlTokenType.DOCUMENT_END || tokenType == YamlTokenType.DIRECTIVES_END)
          return true;

        if (!tokenType.IsComment && !tokenType.IsWhitespace)
          return false;

        cachingLexer.Advance();
      }

      return true;
    }

    private void OpenChameleon()
    {
      Assertion.Assert(!myOpened, "!myOpened");
      AssertSingleChild();

      var openedChameleon = ((IClosedChameleonBody) firstChild).Parse(parser =>
      {
        var yamlParser = (IYamlParser) parser;
        return yamlParser.ParseDocumentBody();
      });

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

    public override string ToString() => "ChameleonDocumentBody";
  }
}