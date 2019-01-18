﻿using System;
using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.TreeBuilder;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing
{
  // General indent error handling tactic:
  // If a node's first token doesn't have correct indentation, don't read the rest of it.
  // If any other part of a node has incorrect indentation, add an error element and reset
  // the expected indentation to be the rest of the element
  // If we don't follow this, we either rollback (and fail to parse the construct at all)
  // or break out of parsing that node and potentially be out of sync for the rest of the file
  internal class YamlTreeStructureBuilder : TreeStructureBuilderBase, IPsiBuilderTokenFactory
  {
    private readonly PsiBuilder myBuilder;
    private bool myCreateClosedChameleons;
    private int myCurrentLineIndent;
    private int myDocumentStartLexeme;
    private bool myExpectImplicitKey;

    public YamlTreeStructureBuilder(ILexer<int> lexer, Lifetime lifetime)
      : base(lifetime)
    {
      myBuilder = new PsiBuilder(lexer, ElementType.YAML_FILE, this, lifetime);
    }

#pragma warning disable 809
    // Micro-optimisation. Makes a small but measurable difference when working with massive files
    [Obsolete("Avoid virtual property call. Use myBuilder field directly")]
    protected override PsiBuilder Builder => myBuilder;
#pragma warning restore 809

    protected override TokenNodeType NewLine => YamlTokenType.NEW_LINE;
    protected override NodeTypeSet CommentsOrWhiteSpacesTokens => YamlTokenType.COMMENTS_OR_WHITE_SPACES;

    public LeafElementBase CreateToken(TokenNodeType tokenNodeType, IBuffer buffer, int startOffset, int endOffset)
    {
      if (tokenNodeType == YamlTokenType.CHAMELEON)
      {
        return new ClosedChameleonElement(YamlTokenType.CHAMELEON, new TreeOffset(startOffset),
          new TreeOffset(endOffset));
      }

      // We define tokens in terms of buffers and ranges to avoid allocation of strings and substrings - nothing is
      // allocated until GetText() is called. Make sure to use the helper methods that work directly with the buffer for
      // string comparison or find operations. If we ever change usage patterns and decide to pre-allocate text strings,
      // please consider interning!
      return tokenNodeType.Create(buffer, new TreeOffset(startOffset), new TreeOffset(endOffset));
    }

    protected override string GetExpectedMessage(string name)
    {
      return ParserMessages.GetExpectedMessage(name);
    }

    public void ParseFile()
    {
      var mark = MarkNoSkipWhitespace();

      do
      {
        ParseDocument();
      } while (!myBuilder.Eof());

      Done(mark, ElementType.YAML_FILE);
    }

    private void ParseDocument()
    {
      // TODO: Can we get indents in this prefix?
      // TODO: Should the document prefix be part of the document node?
      SkipLeadingWhitespace();

      if (myBuilder.Eof())
        return;

      var mark = MarkNoSkipWhitespace();

      ParseDirectives();
      ParseChameleonDocumentBody();

      if (GetTokenTypeNoSkipWhitespace() == YamlTokenType.DOCUMENT_END)
      {
        Advance();
        ParseComments();
      }

      Done(mark, ElementType.YAML_DOCUMENT);
    }

    private void ParseDirectives()
    {
      var tt = GetTokenTypeNoSkipWhitespace();
      if (tt != YamlTokenType.PERCENT && tt != YamlTokenType.DIRECTIVES_END)
        return;

      var mark = MarkNoSkipWhitespace();

      do
      {
        var curr = myBuilder.GetCurrentLexeme();

        ParseDirective();

        if (curr == myBuilder.GetCurrentLexeme())
          break;
      } while (!myBuilder.Eof() && GetTokenTypeNoSkipWhitespace() != YamlTokenType.DIRECTIVES_END);

      if (!myBuilder.Eof())
        ExpectToken(YamlTokenType.DIRECTIVES_END);

      Done(mark, ElementType.DIRECTIVES);
    }

    private void ParseDirective()
    {
      if (GetTokenTypeNoSkipWhitespace() != YamlTokenType.PERCENT)
        return;

      var mark = MarkNoSkipWhitespace();

      ExpectToken(YamlTokenType.PERCENT);
      ExpectTokenNoSkipWhitespace(YamlTokenType.NS_CHARS);

      do
      {
        if (GetTokenTypeNoSkipWhitespace() != YamlTokenType.WHITESPACE)
          break;
        ParseSeparateInLine();
        if (GetTokenTypeNoSkipWhitespace() != YamlTokenType.NS_CHARS)
          break;
        ExpectTokenNoSkipWhitespace(YamlTokenType.NS_CHARS);
      } while (!myBuilder.Eof());

      if (GetTokenTypeNoSkipWhitespace() == YamlTokenType.NEW_LINE)
        Advance();

      // We don't care about the indent here. We're at the start of the doc. The first block
      // node will handle its own indent
      ParseTrailingCommentLines();

      Done(mark, ElementType.DIRECTIVE);
    }

    private void ParseChameleonDocumentBody()
    {
      var mark = MarkNoSkipWhitespace();

      // TODO: We need a better API than this
      // We advance through the lexer once, roll back and then AlterToken will do it all again. The lexer is cached, so
      // it's not that bad, but it would be better to have an overload of AlterToken that can avoid it
      var mark2 = MarkNoSkipWhitespace();
      while (!myBuilder.Eof() && !IsDocumentEnd(GetTokenTypeNoSkipWhitespace()))
        Advance();
      var currentLexeme = myBuilder.GetCurrentLexeme();
      myBuilder.RollbackTo(mark2);
      var count = currentLexeme - myBuilder.GetCurrentLexeme();
      myBuilder.AlterToken(YamlTokenType.CHAMELEON, count);

      Done(mark, YamlChameleonElementTypes.CHAMELEON_DOCUMENT_BODY);
    }

    public void ParseDocumentBody()
    {
      var mark = MarkNoSkipWhitespace();

      ParseRootBlockNode();

      var tt = GetTokenTypeNoSkipWhitespace();
      if (!myBuilder.Eof() && !IsDocumentEnd(tt))
      {
        var errorMark = MarkNoSkipWhitespace();
        while (!myBuilder.Eof() && !IsDocumentEnd(GetTokenTypeNoSkipWhitespace()))
          Advance();
        myBuilder.Error(errorMark, "Unexpected content");
      }

      Done(mark, ElementType.DOCUMENT_BODY);
    }

    private void ParseChameleonRootBlockNode()
    {
      myCreateClosedChameleons = true;
      ParseRootBlockNode();
      myCreateClosedChameleons = false;
    }

    public void ParseRootBlockNode()
    {
      var tt = GetTokenTypeNoSkipWhitespace();
      if (!IsDocumentEnd(tt))
      {
        myDocumentStartLexeme = myBuilder.GetCurrentLexeme();

        // [207] l-bare-document	::=	s-l+block-node(-1,block-in)
        // We know we can safely ignore this return value. It only fails for indent
        // ReSharper disable once MustUseReturnValue
        TryParseBlockNode(-1, true);
      }
    }

    // block-in being "inside a block sequence"
    // NOTE! This method is not guaranteed to consume any tokens! Protect against endless loops!
    // [196] s-l+block-node(n,c)
    [MustUseReturnValue]
    private bool TryParseBlockNode(int expectedIndent, bool isBlockIn)
    {
      return TryParseBlockInBlock(expectedIndent, isBlockIn) || TryParseFlowInBlock(expectedIndent);
    }

    private void ParseBlockIndented(int expectedIndent, bool isBlockIn)
    {
      if (!TryParseCompactNotation(expectedIndent) && !TryParseBlockNode(expectedIndent, isBlockIn))
      {
        Done(MarkNoSkipWhitespace(), ElementType.EMPTY_SCALAR_NODE);
        ParseComments();
      }
    }

    // [198] s-l+block-in-block(n, c)
    private bool TryParseBlockInBlock(int expectedIndent, bool isBlockIn)
    {
      return TryParseBlockScalar(expectedIndent) || TryParseBlockCollection(expectedIndent, isBlockIn);
    }

    // [199] s-l+block-scalar(n, c)
    private bool TryParseBlockScalar(int expectedIndent)
    {
      var mark = MarkNoSkipWhitespace();

      var correctIndent = TryParseSeparationSpaceWithoutRollback(expectedIndent + 1);
      if (correctIndent)
      {
        // Start the node after the whitespace. It's just nicer.
        var scalarMark = MarkNoSkipWhitespace();

        if (TryParseNodeProperties(expectedIndent + 1))
          correctIndent = TryParseSeparationSpaceWithoutRollback(expectedIndent + 1);

        if (!correctIndent)
        {
          ErrorBeforeWhitespaces("Invalid indent");
          expectedIndent = myCurrentLineIndent;
        }

        var tt = GetTokenTypeNoSkipWhitespace();
        if (tt == YamlTokenType.PIPE)
        {
          ParseBlockScalar(expectedIndent, scalarMark, tt, ElementType.LITERAL_SCALAR_NODE);
          myBuilder.Drop(mark);
          return true;
        }

        if (tt == YamlTokenType.GT)
        {
          ParseBlockScalar(expectedIndent, scalarMark, tt, ElementType.FOLDED_SCALAR_NODE);
          myBuilder.Drop(mark);
          return true;
        }
      }

      myBuilder.RollbackTo(mark);
      return false;
    }

    private void ParseBlockScalar(int expectedIndent, int mark, TokenNodeType indicator, CompositeNodeType elementType)
    {
      ExpectToken(indicator);

      // Scalar indent is either calculated from an indentation indicator,
      // or set to -1 to indicate auto-detect
      var scalarIndent = ParseBlockHeader(expectedIndent);
      ParseMultilineScalarText(scalarIndent);

      // Scalars don't consume their trailing whitespace
      DoneBeforeWhitespaces(mark, elementType);
    }

    private void ParseMultilineScalarText(int expectedIndent)
    {
      // Keep track of the end of the value. We'll roll back to here at the
      // end. We only update it when we have valid content, or a valid indent
      // If we get something else (new lines or invalid content) we'll advance
      // but not move this forward, giving us somewhere to roll back to
      var endOfValueMark = MarkNoSkipWhitespace();

      // Skip leading whitespace, but not NEW_LINE or INDENT. Unlikely to get this, tbh
      SkipSingleLineWhitespace();

      var tt = GetTokenTypeNoSkipWhitespace();
      while (tt == YamlTokenType.SCALAR_TEXT || tt == YamlTokenType.INDENT || tt == YamlTokenType.NEW_LINE)
      {
        // Note that the lexer has handled some indent details for us, too.
        // The lexer will create INDENT tokens of any leading whitespace that
        // is equal or greater to the start of the block scalar. If it matches
        // content before the indent, it doesn't get treated as SCALAR_TEXT
        if (expectedIndent == -1 && tt == YamlTokenType.INDENT)
          expectedIndent = GetTokenLength();

        if (tt == YamlTokenType.SCALAR_TEXT || (tt == YamlTokenType.INDENT && GetTokenLength() > expectedIndent) ||
            tt == YamlTokenType.NEW_LINE)
        {
          Advance();

          // Keep track of the last place that we had either valid content or indent
          // We'll roll back to here in the case of e.g. a too-short indent
          myBuilder.Drop(endOfValueMark);
          endOfValueMark = MarkNoSkipWhitespace();
        }
        else
          Advance();

        SkipSingleLineWhitespace();
        tt = GetTokenTypeNoSkipWhitespace();
      }

      myBuilder.RollbackTo(endOfValueMark);
    }

    private int ParseBlockHeader(int expectedIndent)
    {
      var tt = GetTokenTypeNoSkipWhitespace();
      if (tt != YamlTokenType.NS_DEC_DIGIT && tt != YamlTokenType.PLUS && tt != YamlTokenType.MINUS)
        return -1;

      var mark = MarkNoSkipWhitespace();

      int relativeIndent;
      if (tt == YamlTokenType.NS_DEC_DIGIT)
      {
        relativeIndent = ParseDecDigit(expectedIndent);
        ParseChompingIndicator();
      }
      else
      {
        // We already know it's PLUS or MINUS
        ParseChompingIndicator();
        relativeIndent = ParseDecDigit(expectedIndent);
      }

      Done(mark, ElementType.BLOCK_HEADER);

      return relativeIndent;
    }

    private int ParseDecDigit(int expectedIndent)
    {
      if (GetTokenTypeNoSkipWhitespace() == YamlTokenType.NS_DEC_DIGIT)
      {
        int.TryParse(myBuilder.GetTokenText(), out var relativeIndent);
        Advance();
        return expectedIndent + relativeIndent;
      }

      return -1;
    }

    private void ParseChompingIndicator()
    {
      var tt = GetTokenTypeNoSkipWhitespace();
      if (tt == YamlTokenType.PLUS || tt == YamlTokenType.MINUS)
        Advance();
    }

    // [200] s-l+block-collection(n, c)
    private bool TryParseBlockCollection(int expectedIndent, bool isBlockIn)
    {
      var mark = MarkNoSkipWhitespace();
      var blockNodeContentsMark = MarkNoSkipWhitespace();

      var propertiesMark = MarkNoSkipWhitespace();
      var correctIndent = TryParseSeparationSpaceWithoutRollback(expectedIndent + 1);
      if (correctIndent && TryParseNodeProperties(expectedIndent + 1))
        myBuilder.Drop(propertiesMark);
      else
        myBuilder.RollbackTo(propertiesMark);

      ParseComments();

      // ( l+block-sequence(seq-spaces(n,c)) | l+block-mapping(n) )
      // Look ahead to see what the next rule is so that we know what the expected indent should be - a block sequence
      // has a different indent (seq-spaces) to a block mapping (n)
      var tt = LookAheadNextSignificantToken();
      if (tt == YamlTokenType.MINUS)
      {
        myBuilder.Drop(blockNodeContentsMark);

        // Nested block sequences may be indented one less space, because people intuitively see `-` as indent
        var seqSpaces = isBlockIn ? expectedIndent : expectedIndent - 1;
        if (!TryParseBlockSequenceWithoutRollback(seqSpaces))
          myBuilder.RollbackTo(mark);
        else
        {
          Done(mark, ElementType.BLOCK_SEQUENCE_NODE);
          return true;
        }
      }
      else
      {
        var createClosedChameleons = myCreateClosedChameleons;
        myCreateClosedChameleons = false;

        if (!TryParseBlockMappingWithoutRollback(expectedIndent))
          myBuilder.RollbackTo(mark);
        else
        {
          if (createClosedChameleons)
          {
            // Get rid of the markers of everything we've just parsed and create a closed by default chameleon
            var currentLexeme = myBuilder.GetCurrentLexeme();
            myBuilder.RollbackTo(blockNodeContentsMark);

            var count = currentLexeme - myBuilder.GetCurrentLexeme();
            myBuilder.AlterToken(YamlTokenType.CHAMELEON, count);
          }
          else
          {
            myBuilder.Drop(blockNodeContentsMark);
          }

          Done(mark, YamlChameleonElementTypes.CHAMELEON_BLOCK_MAPPING_NODE);

          return true;
        }
      }

      return false;
    }

    // [183] l+block-sequence(n)
    private bool TryParseBlockSequenceWithoutRollback(int expectedIndent)
    {
      // Figure out the m in s-indent(n+m)
      var m = DetectCollectionIndent(expectedIndent);
      expectedIndent += m;

      // s-indent(n+m)
      if (!TryParseIndentWithoutRollback(expectedIndent))
        return false;

      ParseBlockSequenceEntry(expectedIndent);

      var mark = MarkNoSkipWhitespace();
      do
      {
        if (!TryParseIndentWithoutRollback(expectedIndent))
          break;

        if (GetTokenTypeNoSkipWhitespace() != YamlTokenType.MINUS)
          break;

        ParseBlockSequenceEntry(expectedIndent);
        myBuilder.Drop(mark);
        mark = MarkNoSkipWhitespace();
      } while (!myBuilder.Eof());

      myBuilder.RollbackTo(mark);
      return true;
    }

    private void ParseBlockSequenceEntry(int expectedIndent)
    {
      var mark = MarkNoSkipWhitespace();
      ExpectToken(YamlTokenType.MINUS);
      ParseBlockIndented(expectedIndent, true); // block-in

      Done(mark, ElementType.SEQUENCE_ENTRY);
    }

    private bool TryParseBlockMappingWithoutRollback(int expectedIndent)
    {
      // s-indent(n+m)
      var m = DetectCollectionIndent(expectedIndent);
      expectedIndent += m;

      if (!TryParseIndentWithoutRollback(expectedIndent))
        return false;

      if (!TryParseBlockMapEntry(expectedIndent))
        return false;

      do
      {
        var curr = myBuilder.GetCurrentLexeme();

        var mark = MarkNoSkipWhitespace();
        if (!TryParseIndentWithoutRollback(expectedIndent))
        {
          myBuilder.RollbackTo(mark);
          return true;
        }

        myBuilder.Drop(mark);

        if (!TryParseBlockMapEntry(expectedIndent))
          break;

        if (curr == myBuilder.GetCurrentLexeme())
          break;
      } while (!myBuilder.Eof());

      return true;
    }

    [MustUseReturnValue]
    private bool TryParseBlockMapEntry(int expectedIndent)
    {
      return TryParseBlockMapExplicitEntry(expectedIndent) || TryParseBlockMapImplicitEntry(expectedIndent);
    }

    [MustUseReturnValue]
    private bool TryParseBlockMapExplicitEntry(int expectedIndent)
    {
      if (GetTokenTypeNoSkipWhitespace() == YamlTokenType.QUESTION)
      {
        var mark = MarkNoSkipWhitespace();

        Advance();

        // block-out
        ParseBlockIndented(expectedIndent, false);

        var valueMark = MarkNoSkipWhitespace();

        if (TryParseIndentWithoutRollback(expectedIndent)
            && GetTokenTypeNoSkipWhitespace() == YamlTokenType.COLON)
        {
          Advance();
          ParseBlockIndented(expectedIndent, false);
          myBuilder.Drop(valueMark);
        }
        else
          Done(valueMark, ElementType.EMPTY_SCALAR_NODE);

        Done(mark, ElementType.BLOCK_MAPPING_ENTRY);
        return true;
      }

      return false;
    }

    [MustUseReturnValue]
    private bool TryParseBlockMapImplicitEntry(int expectedIndent)
    {
      myExpectImplicitKey = true;

      var mark = MarkNoSkipWhitespace();

      ParseFlowNode(0);
      ParseSeparateInLine();

      myExpectImplicitKey = false;

      if (GetTokenTypeNoSkipWhitespace() == YamlTokenType.COLON)
      {
        Advance();
        if (!TryParseBlockNode(expectedIndent, false))
        {
          Done(MarkNoSkipWhitespace(), ElementType.EMPTY_SCALAR_NODE);
          ParseComments();
        }

        Done(mark, ElementType.BLOCK_MAPPING_ENTRY);
        return true;
      }

      myBuilder.RollbackTo(mark);
      return false;
    }

    private bool TryParseCompactNotation(int expectedIndent)
    {
      var mark = MarkNoSkipWhitespace();

      var m = DetectInlineIndent();
      if (TryParseIndentWithoutRollback(m)
          && (TryParseCompactSequence(expectedIndent + 1 + m) || TryParseCompactMapping(expectedIndent + 1 + m)))
      {
        myBuilder.Drop(mark);
        return true;
      }

      myBuilder.RollbackTo(mark);
      return false;
    }

    private bool TryParseCompactSequence(int expectedIndent)
    {
      if (GetTokenTypeNoSkipWhitespace() == YamlTokenType.MINUS)
      {
        var mark = MarkNoSkipWhitespace();
        ParseCompactSequenceEntries(expectedIndent);
        // TODO: Do we need a COMPACT_SEQUENCE_NODE?
        Done(mark, ElementType.BLOCK_SEQUENCE_NODE);
        return true;
      }

      return false;
    }

    private void ParseCompactSequenceEntries(int expectedIndent)
    {
      do
      {
        ParseBlockSequenceEntry(expectedIndent);

        var mark = MarkNoSkipWhitespace();
        if (!TryParseIndentWithoutRollback(expectedIndent))
        {
          myBuilder.RollbackTo(mark);
          return;
        }

        myBuilder.Drop(mark);
      } while (!myBuilder.Eof() && GetTokenTypeNoSkipWhitespace() == YamlTokenType.MINUS);
    }

    private bool TryParseCompactMapping(int expectedIndent)
    {
      var mark = MarkNoSkipWhitespace();

      if (TryParseBlockMapEntry(expectedIndent))
      {
        do
        {
          var indentMark = MarkNoSkipWhitespace();
          if (!TryParseIndentWithoutRollback(expectedIndent))
          {
            myBuilder.RollbackTo(indentMark);
            break;
          }

          if (!TryParseBlockMapEntry(expectedIndent))
          {
            myBuilder.RollbackTo(indentMark);
            break;
          }

          myBuilder.Drop(indentMark);
        } while (!myBuilder.Eof());

        // TODO: Do we need a COMPACT_MAPPING_NODE?
        Done(mark, ElementType.BLOCK_MAPPING_NODE);

        return true;
      }

      myBuilder.RollbackTo(mark);

      return false;
    }

    // [197] s-l+flow-in-block(n)
    [MustUseReturnValue]
    private bool TryParseFlowInBlock(int expectedIndent)
    {
      var mark = MarkNoSkipWhitespace();

      var correctIndent = TryParseSeparationSpaceWithoutRollback(expectedIndent + 1);
      if (!correctIndent)
      {
        myBuilder.RollbackTo(mark);
        return false;
      }

      ParseFlowNode(expectedIndent + 1);
      ParseComments();

      myBuilder.Drop(mark);
      return true;
    }

    private void ParseFlowNode(int expectedIndent)
    {
      var tt = GetTokenTypeNoSkipWhitespace();
      if (tt == YamlTokenType.ASTERISK)
        ParseAliasNode();
      else
        ParseFlowContent(expectedIndent);
    }

    private void ParseAliasNode()
    {
      var mark = MarkNoSkipWhitespace();
      ExpectToken(YamlTokenType.ASTERISK);
      ExpectTokenNoSkipWhitespace(YamlTokenType.NS_ANCHOR_NAME);
      Done(mark, ElementType.ALIAS_NODE);
    }

    // [96]	c-ns-properties(n,c)
    private bool TryParseNodeProperties(int expectedIndent)
    {
      var tt = GetTokenTypeNoSkipWhitespace();
      if (tt != YamlTokenType.BANG && tt != YamlTokenType.BANG_LT && tt != YamlTokenType.AMP)
        return false;

      var mark = MarkNoSkipWhitespace();

      // TODO: IJ parser will handle multiple anchor/tags as errors
      if (tt == YamlTokenType.BANG || tt == YamlTokenType.BANG_LT)
      {
        ParseTagProperty();
        ParseAnchorProperty(expectedIndent);
      }
      else if (tt == YamlTokenType.AMP)
      {
        ParseAnchorProperty();
        ParseTagProperty(expectedIndent);
      }

      // Unity scene files are sometimes invalid, with an extra keyword after the node properties and before a block
      // mapping. We'll silently eat it.
      // E.g.:
      // --- !u!4 &154806035 stripped
      // Transform:
      //   m_PrefabParentObject: ...
      if (GetTokenTypeNoSkipWhitespace().IsWhitespace && IsPlainScalarToken(LookAheadNoSkipWhitespaces(1)) &&
          CompareLookAheadText(1, "stripped"))
      {
        Advance(); // <whitespace>
        Advance(); // stripped
      }

      Done(mark, ElementType.NODE_PROPERTIES);
      return true;
    }

    private void ParseAnchorProperty(int expectedIndent = -1)
    {
      var mark = MarkNoSkipWhitespace();

      // Only parse indent if we're in between properties
      var correctIndent = true;
      if (expectedIndent != -1)
        correctIndent = TryParseSeparationSpaceWithoutRollback(expectedIndent);

      var tt = GetTokenTypeNoSkipWhitespace();
      if (tt == YamlTokenType.AMP && correctIndent)
      {
        var anchorMark = MarkNoSkipWhitespace();
        ExpectToken(YamlTokenType.AMP);
        ExpectTokenNoSkipWhitespace(YamlTokenType.NS_ANCHOR_NAME);
        Done(anchorMark, ElementType.ANCHOR_PROPERTY);
        myBuilder.Drop(mark);
        return;
      }

      myBuilder.RollbackTo(mark);
    }

    private void ParseTagProperty(int expectedIndent = -1)
    {
      var mark = MarkNoSkipWhitespace();

      // Only parse indent if we're in between properties
      var correctIndent = true;
      if (expectedIndent != -1)
        correctIndent = TryParseSeparationSpaceWithoutRollback(expectedIndent);

      var tt = GetTokenTypeNoSkipWhitespace();
      if (tt == YamlTokenType.BANG_LT && correctIndent)
      {
        ParseVerbatimTagProperty(mark);
        return;
      }

      if (tt == YamlTokenType.BANG && correctIndent)
      {
        if (LookAheadNoSkipWhitespaces(1).IsWhitespace)
          ParseNonSpecificTagProperty(mark);
        else
          ParseShorthandTagProperty(mark);
        return;
      }

      myBuilder.RollbackTo(mark);
    }

    private void ParseVerbatimTagProperty(int mark)
    {
      ExpectToken(YamlTokenType.BANG_LT);
      ExpectTokenNoSkipWhitespace(YamlTokenType.NS_URI_CHARS);
      ExpectTokenNoSkipWhitespace(YamlTokenType.GT);
      Done(mark, ElementType.VERBATIM_TAG_PROPERTY);
    }

    private void ParseShorthandTagProperty(int mark)
    {
      ParseTagHandle();
      var tt = GetTokenTypeNoSkipWhitespace();
      // TODO: Is TAG_CHARS a superset of ns-plain?
      // TODO: Perhaps we should accept all text and add an inspection for invalid chars?
      if (tt != YamlTokenType.NS_TAG_CHARS && tt != YamlTokenType.NS_PLAIN_ONE_LINE)
        ErrorBeforeWhitespaces(ParserMessages.GetExpectedMessage("text"));
      else
        Advance();
      Done(mark, ElementType.SHORTHAND_TAG_PROPERTY);
    }

    private void ParseTagHandle()
    {
      var mark = MarkNoSkipWhitespace();
      ExpectToken(YamlTokenType.BANG);
      var elementType = ParseSecondaryOrNamedTagHandle();
      Done(mark, elementType);
    }

    private CompositeNodeType ParseSecondaryOrNamedTagHandle()
    {
      // Make sure we don't try to match a primary tag handle followed by ns-plain. E.g. `!foo`
      var tt = GetTokenTypeNoSkipWhitespace();
      var la = LookAheadNoSkipWhitespaces(1);
      if (tt.IsWhitespace || ((tt == YamlTokenType.NS_WORD_CHARS || tt == YamlTokenType.NS_TAG_CHARS) &&
                              la != YamlTokenType.BANG))
      {
        return ElementType.PRIMARY_TAG_HANDLE;
      }

      if (tt != YamlTokenType.NS_WORD_CHARS && tt != YamlTokenType.NS_TAG_CHARS && tt != YamlTokenType.BANG)
      {
        ErrorBeforeWhitespaces(ParserMessages.GetExpectedMessage("text", YamlTokenType.BANG.TokenRepresentation));
        return ElementType.NAMED_TAG_HANDLE;
      }

      var elementType = ElementType.SECONDARY_TAG_HANDLE;
      if (tt != YamlTokenType.BANG)
      {
        Advance(); // CHARS
        elementType = ElementType.NAMED_TAG_HANDLE;
      }

      ExpectTokenNoSkipWhitespace(YamlTokenType.BANG);

      return elementType;
    }

    private void ParseNonSpecificTagProperty(int mark)
    {
      ExpectToken(YamlTokenType.BANG);
      Done(mark, ElementType.NON_SPECIFIC_TAG_PROPERTY);
    }

    private void ParseFlowContent(int expectedIndent)
    {
      var mark = MarkNoSkipWhitespace();

      // We're already expected to be at the correct indent, ParseFlowNode has checked
      var correctIndent = true;
      if (TryParseNodeProperties(expectedIndent))
        correctIndent = TryParseSeparationSpaceWithoutRollback(expectedIndent);

      if (!correctIndent && IsNonEmptyFlowContent())
      {
        ErrorBeforeWhitespaces("Invalid indent");
        expectedIndent = myCurrentLineIndent;
      }

      var tt = GetTokenTypeNoSkipWhitespace();

      if (IsReservedIndicator(tt))
      {
        MarkErrorAndSkipToken("Reserved indicator cannot be used");
        tt = GetTokenTypeNoSkipWhitespace();
      }

      CompositeNodeType elementType = null;
      if (tt == YamlTokenType.LBRACK)
        elementType = ParseFlowSequence(expectedIndent);
      else if (tt == YamlTokenType.LBRACE)
        elementType = ParseFlowMapping(expectedIndent);
      else if (IsDoubleQuoted(tt))
      {
        Advance();
        elementType = ElementType.DOUBLE_QUOTED_SCALAR_NODE;
      }
      else if (IsSingleQuoted(tt))
      {
        Advance();
        elementType = ElementType.SINGLE_QUOTED_SCALAR_NODE;
      }
      else if (IsPlainScalarToken(tt))
        elementType = ParseMultilinePlainScalar(expectedIndent);
      else if (tt != YamlTokenType.DOCUMENT_END)
        elementType = ElementType.EMPTY_SCALAR_NODE;

      if (elementType != null)
        Done(mark, elementType);
      else
        myBuilder.Drop(mark);
    }

    private bool IsNonEmptyFlowContent()
    {
      var tt = GetTokenTypeNoSkipWhitespace();
      return tt == YamlTokenType.LBRACK || tt == YamlTokenType.RBRACK
                                        || IsDoubleQuoted(tt) || IsSingleQuoted(tt) || IsPlainScalarToken(tt);
    }

    private CompositeNodeType ParseFlowSequence(int expectedIndent)
    {
      ExpectToken(YamlTokenType.LBRACK);

      if (!ParseOptionalSeparationSpace(expectedIndent))
      {
        ErrorBeforeWhitespaces("Invalid indent");
        expectedIndent = myCurrentLineIndent;
      }

      if (GetTokenTypeNoSkipWhitespace() == YamlTokenType.RBRACK)
      {
        Advance();
        return ElementType.FLOW_SEQUENCE_NODE;
      }

      ParseFlowSequenceEntry(expectedIndent);

      // Don't update expectedIndent - we have closing indicators, so we should know
      // where things are
      if (!ParseOptionalSeparationSpace(expectedIndent))
        ErrorBeforeWhitespaces("Invalid indent");

      if (GetTokenTypeNoSkipWhitespace() == YamlTokenType.COMMA)
      {
        do
        {
          ExpectToken(YamlTokenType.COMMA);

          if (!ParseOptionalSeparationSpace(expectedIndent))
            ErrorBeforeWhitespaces("Invalid indent");

          if (GetTokenTypeNoSkipWhitespace() != YamlTokenType.RBRACK)
          {
            ParseFlowSequenceEntry(expectedIndent);

            if (!ParseOptionalSeparationSpace(expectedIndent))
              ErrorBeforeWhitespaces("Invalid indent");
          }
        } while (!myBuilder.Eof() && GetTokenTypeNoSkipWhitespace() != YamlTokenType.RBRACK &&
                 GetTokenTypeNoSkipWhitespace() == YamlTokenType.COMMA);
      }

      ExpectToken(YamlTokenType.RBRACK);

      return ElementType.FLOW_SEQUENCE_NODE;
    }

    private void ParseFlowSequenceEntry(int expectedIndent)
    {
      var mark = MarkNoSkipWhitespace();

      if (!TryParseFlowPair(expectedIndent))
        ParseFlowNode(expectedIndent);

      Done(mark, ElementType.FLOW_SEQUENCE_ENTRY);
    }

    private bool TryParseFlowPair(int expectedIndent)
    {
      if (GetTokenTypeNoSkipWhitespace() == YamlTokenType.QUESTION)
      {
        ParseFlowMapExplicitEntry(expectedIndent);
        return true;
      }

      var mark = MarkNoSkipWhitespace();

      myExpectImplicitKey = true;

      ParseFlowContent(expectedIndent);

      myExpectImplicitKey = false;

      if (ParseOptionalSeparationSpace(expectedIndent) && GetTokenTypeNoSkipWhitespace() == YamlTokenType.COLON)
      {
        ExpectTokenNoSkipWhitespace(YamlTokenType.COLON);

        if (ParseOptionalSeparationSpace(expectedIndent))
          ParseFlowNode(expectedIndent);
        else
          Done(MarkNoSkipWhitespace(), ElementType.EMPTY_SCALAR_NODE);

        Done(mark, ElementType.FLOW_PAIR);
        return true;
      }

      myBuilder.RollbackTo(mark);
      return false;
    }

    private void ParseFlowMapExplicitEntry(int expectedIndent)
    {
      var mark = MarkNoSkipWhitespace();

      ExpectTokenNoSkipWhitespace(YamlTokenType.QUESTION);

      if (!TryParseSeparationSpaceWithoutRollback(expectedIndent))
      {
        ErrorBeforeWhitespaces("Invalid indent");
        myCurrentLineIndent = expectedIndent;
      }

      if (GetTokenTypeNoSkipWhitespace() != YamlTokenType.COLON)
        ParseFlowNode(expectedIndent);
      else
      {
        Advance();
        Done(MarkNoSkipWhitespace(), ElementType.EMPTY_SCALAR_NODE);
      }

      var valueMark = MarkNoSkipWhitespace();
      if (!ParseOptionalSeparationSpace(expectedIndent))
      {
        ErrorBeforeWhitespaces("Invalid indent");
        expectedIndent = myCurrentLineIndent;
      }

      if (GetTokenTypeNoSkipWhitespace() != YamlTokenType.COLON)
      {
        myBuilder.RollbackTo(valueMark);
        Done(MarkNoSkipWhitespace(), ElementType.EMPTY_SCALAR_NODE);
      }
      else
      {
        myBuilder.Drop(valueMark);

        ExpectTokenNoSkipWhitespace(YamlTokenType.COLON);

        if (TryParseSeparationSpaceWithoutRollback(expectedIndent))
          ParseFlowContent(expectedIndent);
        else
          Done(MarkNoSkipWhitespace(), ElementType.EMPTY_SCALAR_NODE);
      }

      Done(mark, ElementType.FLOW_MAP_ENTRY);
    }

    private void ParseFlowMapImplicitEntry(int expectedIndent)
    {
      var mark = MarkNoSkipWhitespace();

      ParseFlowContent(expectedIndent);

      var valueMark = MarkNoSkipWhitespace();
      if (!ParseOptionalSeparationSpace(expectedIndent))
      {
        ErrorBeforeWhitespaces("Invalid indent");
        expectedIndent = myCurrentLineIndent;
      }

      if (GetTokenTypeNoSkipWhitespace() != YamlTokenType.COLON)
      {
        myBuilder.RollbackTo(valueMark);
        Done(MarkNoSkipWhitespace(), ElementType.EMPTY_SCALAR_NODE);
      }
      else
      {
        myBuilder.Drop(valueMark);

        ExpectTokenNoSkipWhitespace(YamlTokenType.COLON);

        if (ParseOptionalSeparationSpace(expectedIndent))
          ParseFlowNode(expectedIndent);
        else
          Done(MarkNoSkipWhitespace(), ElementType.EMPTY_SCALAR_NODE);
      }

      Done(mark, ElementType.FLOW_MAP_ENTRY);
    }

    private CompositeNodeType ParseFlowMapping(int expectedIndent)
    {
      ExpectToken(YamlTokenType.LBRACE);

      if (!ParseOptionalSeparationSpace(expectedIndent))
      {
        ErrorBeforeWhitespaces("Invalid indent");
        expectedIndent = myCurrentLineIndent;
      }

      if (GetTokenTypeNoSkipWhitespace() == YamlTokenType.RBRACE)
      {
        Advance();
        return ElementType.FLOW_MAPPING_NODE;
      }

      ParseFlowMapEntry(expectedIndent);

      // Don't update expectedIndent - we have closing indicators, so we should know
      // where things are
      if (!ParseOptionalSeparationSpace(expectedIndent))
        ErrorBeforeWhitespaces("Invalid indent");

      if (GetTokenTypeNoSkipWhitespace() == YamlTokenType.COMMA)
      {
        do
        {
          ExpectToken(YamlTokenType.COMMA);

          if (!ParseOptionalSeparationSpace(expectedIndent))
            ErrorBeforeWhitespaces("Invalid indent");

          if (GetTokenTypeNoSkipWhitespace() != YamlTokenType.RBRACE)
          {
            ParseFlowMapEntry(expectedIndent);

            if (!ParseOptionalSeparationSpace(expectedIndent))
              ErrorBeforeWhitespaces("Invalid indent");
          }
        } while (!myBuilder.Eof() && GetTokenTypeNoSkipWhitespace() != YamlTokenType.RBRACE &&
                 GetTokenTypeNoSkipWhitespace() == YamlTokenType.COMMA);
      }

      ExpectToken(YamlTokenType.RBRACE);

      return ElementType.FLOW_MAPPING_NODE;
    }

    private void ParseFlowMapEntry(int expectedIndent)
    {
      if (GetTokenTypeNoSkipWhitespace() == YamlTokenType.QUESTION)
        ParseFlowMapExplicitEntry(expectedIndent);
      else
        ParseFlowMapImplicitEntry(expectedIndent);
    }

    private CompositeNodeType ParseMultilinePlainScalar(int expectedIndent)
    {
      var endOfValueMark = -1;

      var tt = GetTokenTypeNoSkipWhitespace();
      while (IsPlainScalarToken(tt) || tt == YamlTokenType.INDENT || tt == YamlTokenType.NEW_LINE)
      {
        if (myExpectImplicitKey && tt == YamlTokenType.NEW_LINE)
          break;

        Advance();

        if (IsPlainScalarToken(tt))
        {
          if (endOfValueMark != -1 && myCurrentLineIndent < expectedIndent)
            break;

          if (endOfValueMark != -1)
            myBuilder.Drop(endOfValueMark);
          endOfValueMark = MarkNoSkipWhitespace();
        }

        SkipSingleLineWhitespace();
        tt = GetTokenTypeNoSkipWhitespace();
      }

      if (endOfValueMark != -1)
        myBuilder.RollbackTo(endOfValueMark);

      return ElementType.PLAIN_SCALAR_NODE;
    }

    private bool IsPlainScalarToken(TokenNodeType tt)
    {
      if (myExpectImplicitKey)
        return tt == YamlTokenType.NS_PLAIN_ONE_LINE;
      return tt == YamlTokenType.NS_PLAIN_ONE_LINE || tt == YamlTokenType.NS_PLAIN_MULTI_LINE;
    }

    private new void Advance()
    {
      if (myBuilder.Eof())
        return;

      var tt = myBuilder.GetTokenType();
      if (tt == YamlTokenType.NEW_LINE)
        myCurrentLineIndent = 0;
      else if (tt == YamlTokenType.INDENT)
        myCurrentLineIndent = GetTokenLength();
      base.Advance();
    }

    protected override void SkipWhitespaces()
    {
      base.SkipWhitespaces();
      if (JustSkippedNewLine)
        myCurrentLineIndent = 0;
    }

    private int GetTokenLength()
    {
      var token = myBuilder.GetToken();
      return token.End - token.Start;
    }

    // ReSharper disable once UnusedMember.Local
    [Obsolete("Skips whitespace. Be explicit and call GetTokenTypeNoSkipWhitespace", true)]
    private new TokenNodeType GetTokenType()
    {
      throw new InvalidOperationException("Do not call - be explicit about whitespace");
    }

    private TokenNodeType GetTokenTypeNoSkipWhitespace()
    {
      // this.GetTokenType() calls SkipWhitespace() first
      return myBuilder.GetTokenType();
    }

    // ReSharper disable once UnusedMethodReturnValue.Local - mirrors the ExpectToken API
    private bool ExpectTokenNoSkipWhitespace(NodeType token, bool dontSkipSpacesAfter = false)
    {
      if (GetTokenTypeNoSkipWhitespace() != token)
      {
        var message = (token as TokenNodeType)?.GetDescription() ?? token.ToString();
        ErrorBeforeWhitespaces(GetExpectedMessage(message));
        return false;
      }

      if (dontSkipSpacesAfter)
        myBuilder.AdvanceLexer();
      else
        Advance();
      return true;
    }

    // ReSharper disable once UnusedMember.Local
    [Obsolete("Skips whitespace. Be explicit and call MarkSkipWhitespace", true)]
    private new int Mark()
    {
      throw new InvalidOperationException("Do not call Mark - be explicit about whitespace");
    }

    // ReSharper disable once UnusedMember.Local
    [MustUseReturnValue]
    private int MarkSkipWhitespace()
    {
      return base.Mark();
    }

    [MustUseReturnValue]
    private int MarkNoSkipWhitespace()
    {
      // this.Mark() calls SkipWhitespace() first
      return myBuilder.Mark();
    }


    private TokenNodeType LookAheadNextSignificantToken()
    {
      var tt = GetTokenTypeNoSkipWhitespace();
      var i = 1;
      while (tt == YamlTokenType.INDENT)
        tt = LookAheadNoSkipWhitespaces(i++);
      return tt;
    }


    private bool CompareLookAheadText(int index, string text)
    {
      return myBuilder.CompareTokenText(myBuilder.GetCurrentLexeme() + index, text);
    }


    // According to the spec:
    // s-indent(n+m)
    // For some auto-detected m > 0
    // This isn't really explained further but essentially means the new indent must be greater than the current indent,
    // and we have to figure out what it is so it can be applied to allow following rules to get a consistent indent for
    // e.g. all items in a series of block mapping entries. So that means lookahead to the next significant token and
    // that gives us the indent (n+m). We already known n, so this gives us m
    private int DetectCollectionIndent(int expectedIndent)
    {
      var tt = GetTokenTypeNoSkipWhitespace();
      var currentLineIndent = 0;

      // The lexer should give us the whitespace. Fall back to counting whitespace, just in case
      if (tt == YamlTokenType.INDENT)
        currentLineIndent = GetTokenLength();
      else
      {
        while (myBuilder.GetTokenType(currentLineIndent) == YamlTokenType.WHITESPACE)
          currentLineIndent++;
      }

      // currentLineIndent is (n+m), work out m, make sure it's greater than 0
      return Math.Max(-expectedIndent + currentLineIndent, 1);
    }

    private int DetectInlineIndent()
    {
      var m = 0;
      while (myBuilder.GetTokenType(m) == YamlTokenType.WHITESPACE)
        m++;

      return Math.Max(1, m);
    }

    // [79] s-l-comments
    private void ParseComments()
    {
      var eolMark = -1;
      while (!myBuilder.Eof() && IsWhitespaceNewLineIndentOrComment())
      {
        var isNewLine = GetTokenTypeNoSkipWhitespace() == NewLine;
        Advance();
        if (isNewLine)
        {
          if (eolMark != -1)
            myBuilder.Drop(eolMark);
          eolMark = MarkNoSkipWhitespace();
        }
      }

      if (eolMark != -1)
        myBuilder.RollbackTo(eolMark);
    }

    // We don't care about the final indent. The next construct will have
    // to assert it itself
    private void ParseTrailingCommentLines()
    {
      _EatWhitespaceAndIndent();
    }

    // s-indent(n)
    // Note that s-indent(m) ("for some auto-detected m") needs to be auto-detected first
    [MustUseReturnValue]
    private bool TryParseIndentWithoutRollback(int expectedIndent)
    {
      if (GetTokenTypeNoSkipWhitespace() == YamlTokenType.INDENT)
      {
        if (GetTokenLength() >= expectedIndent)
        {
          Advance();
          return true;
        }
      }
      else if (GetTokenTypeNoSkipWhitespace() == YamlTokenType.WHITESPACE)
      {
        for (var i = 0; i < expectedIndent; i++)
        {
          var token = myBuilder.GetTokenType(i);
          if (token != null && token != YamlTokenType.WHITESPACE)
            return false;
        }

        for (var i = 0; i < expectedIndent; i++)
          Advance();
        return true;
      }

      return expectedIndent == 0;
    }

    // [66] s-separate-in-line
    private void ParseSeparateInLine()
    {
      while (!myBuilder.Eof())
      {
        var tt = GetTokenTypeNoSkipWhitespace();
        if (tt == YamlTokenType.NEW_LINE || !tt.IsWhitespace)
          return;

        Advance();
      }
    }

    // [80] s-separate(n, c)
    [MustUseReturnValue]
    private bool TryParseSeparationSpaceWithoutRollback(int expectedIndent)
    {
      // I.e. c is block-key or flow-key
      if (myExpectImplicitKey)
      {
        ParseSeparateInLine();
        return true;
      }

      var curr = myBuilder.GetCurrentLexeme();

      // Either skip whitespace on the same line, or skip
      // empty lines, whitespace, comments and indents. If
      // ending on different line, must match expectedIndent
      var seenNewLine = false;
      while (!myBuilder.Eof() && IsWhitespaceNewLineIndentOrComment())
      {
        if (GetTokenTypeNoSkipWhitespace() == YamlTokenType.NEW_LINE)
          seenNewLine = true;
        Advance();
      }

      // Seen a new line, therefore must be indented correctly
      var isStartOfDocument = curr == myDocumentStartLexeme;
      if (seenNewLine || isStartOfDocument)
        return myCurrentLineIndent >= expectedIndent;

      // If not on a newline, must have consumed at least one whitespace char
      return myBuilder.GetCurrentLexeme() != curr;
    }

    [MustUseReturnValue]
    private bool ParseOptionalSeparationSpace(int expectedIndent)
    {
      if (myExpectImplicitKey)
      {
        while (!myBuilder.Eof() && GetTokenTypeNoSkipWhitespace() == YamlTokenType.WHITESPACE)
          Advance();
        return true;
      }

      // TODO: Should we rollback?
      return !IsWhitespaceNewLineIndentOrComment() || TryParseSeparationSpaceWithoutRollback(expectedIndent);
    }

    private void SkipLeadingWhitespace()
    {
      _EatWhitespaceAndIndent();
    }

    // If you're calling this, you're probably calling the wrong method
    private void _EatWhitespaceAndIndent()
    {
      while (!myBuilder.Eof() && IsWhitespaceNewLineIndentOrComment())
        Advance();
    }

    private bool IsWhitespaceNewLineIndentOrComment()
    {
      // GetTokenType skips WS, NL and comments, but let's be explicit
      var tt = GetTokenTypeNoSkipWhitespace();
      return tt == YamlTokenType.INDENT || tt == YamlTokenType.NEW_LINE || tt.IsComment || tt.IsWhitespace;
    }

    private void SkipSingleLineWhitespace()
    {
      while (!myBuilder.Eof())
      {
        var tt = GetTokenTypeNoSkipWhitespace();
        if (tt == YamlTokenType.NEW_LINE || (!tt.IsWhitespace && !tt.IsComment))
          return;

        Advance();
      }
    }

    private bool IsDoubleQuoted(TokenNodeType tt)
    {
      return tt == YamlTokenType.C_DOUBLE_QUOTED_MULTI_LINE || tt == YamlTokenType.C_DOUBLE_QUOTED_SINGLE_LINE;
    }

    private bool IsSingleQuoted(TokenNodeType tt)
    {
      return tt == YamlTokenType.C_SINGLE_QUOTED_MULTI_LINE || tt == YamlTokenType.C_SINGLE_QUOTED_SINGLE_LINE;
    }

    private bool IsReservedIndicator(TokenNodeType tt)
    {
      return tt == YamlTokenType.AT || tt == YamlTokenType.BACKTICK;
    }

    private bool IsDocumentEnd(TokenNodeType tt)
    {
      // DIRECTIVES_END means the start of the next document
      return tt == YamlTokenType.DOCUMENT_END || tt == YamlTokenType.DIRECTIVES_END;
    }
  }
}