using System;
using System.Text.RegularExpressions;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing
{
  // A note about the contexts, as defined in the spec, because they are confusing.
  // The rules of the spec define contexts that control how multi-line scalars and
  // whitespace work. The problem is that they are not switches - you don't hit a
  // particular token and switch to another context. Instead, once you've matched
  // a rule, you're in the context that the rule is defined in.
  // For example, the spec starts in `block-in`. The next token might be part of a
  // block node (e.g. the indicator for a literal or folded block scalar), or it
  // might be from a flow node (e.g. single/double quotes, plain scalar, LBRACK or
  // LBRACE). The problem is that we've started in `block-in` and you can only
  // match a flow node when you're in `flow-out`, but there is no discrete switch
  // to get us into `flow-out`. By matching the flow node, we're ALREADY, implicitly
  // in `flow-out`. (We'd have to match either an alias, or tag properties, which
  // are defined as `flow-out` while we're still in `block-in`)
  // Another example is block mapping. We start in `block-in`. If we match QUEST,
  // we switch to `block-out` to match a block indented node. But this node might
  // be a flow node, such as a simple/implicit key. But that can only match if
  // we're in `flow-key` context. So there is no switch to `flow-key`. By matching
  // the construct, we're implicitly ALREADY in `flow-key`.
  // One more: if the first thing we match (in `block-in`) is a plain scalar, then
  // it could be a block mapping node with a simple/implicit key (which means we're
  // in `flow-key` context) or it could be a flow node with a plain scalar, which
  // means we're in `flow-out` context. The only way to know what context we're
  // actually in is to continue lexing to see if we get a COLON or not.
  //
  // In other words, we don't switch to a context and match, instead we match tokens
  // and that tells us what context we're in. Which means we can't map contexts to
  // lexer states, and that makes it very difficult to know if we're properly
  // following the spec.
  //
  // Phew.
  //
  // Fortunately, all is not lost. By studying the spec, the contexts mostly dictate
  // how we handle plain text - the `ns-plain` rule. It can be summed up:
  //
  // * `ns-plain(block-key)` = multi-line, any char
  // * `ns-plain(flow-in)` = multi-line, safe chars
  // * `ns-plain(flow-out)` = multi-line, any char
  // * `ns-plain(flow-key)` = single-line, safe chars
  //
  // As an overview, the spec starts in `block-in` and then:
  //
  // * Block scalars don't use `ns-plain`, handle themselves and stay in `block-in`
  // * Block sequence entries stay in `block-in` until they hit something more interesting
  // * Block sequence entry is MINUS followed by:
  //   * block-node (recursive, boring)
  //   * flow-node (see below)
  // * Block mapping
  //   * Explicit entry has key + value indicators. Key and value nodes are `block-out`
  //   * Implicit key (json) - boring
  //   * Implicit key (yaml) - `ns-plain(block-key)` (multi-line, any char)
  // * Flow scalar - `ns-plain(flow-out)` (multi-line, any char)
  // * Flow map entry key - `flow-in` (after LBRACE)
  //   * Explicit entry has key + value indicators. Key and value nodes are `flow-in` (multi-line, safe chars)
  //   * Implicit entry has value indicator, but not key. Key and value nodes are `flow-in` (multi-line, safe chars)
  // * Flow sequence entry - `flow-in` (after LBRACK)
  //   * Scalar entry - `ns-plain(flow-in)` (multi-line, safe char)
  //   * Flow-pair (compact notation)
  //     * Explicit - QUEST scalar (`ns-plain(flow-in)` - multi-line, safe char)
  //                  optional (COLON `ns-plain(flow-in)` - multi-line, safe char)
  //     * Implicit - `ns-plain(flow-key)` - single line, safe char
  //
  // All of which means we can simplify things. The safe char/any char thing is dependent
  // on being in a FLOW or BLOCK context. This is easily tracked with a level incremented
  // by LBRACK and LBRACE. Single line/multi-line is all about implicit key. We can summarise:
  //
  // If in BLOCK:
  //   Match (single line, any char) followed by COLON -> implicit block key
  //   Everything else is `flow-out` -> multi-line, any char
  // If in FLOW:
  //   Match (single line, safe char) followed by COLON -> implicit flow key
  //   Everything else is `flow-in` -> multi-line, safe char
  //
  // Furthermore, we can't match multi-line `ns-plain` in this lexer, as it requires a better
  // knowledge of the indent rules than we have - if the indent is less than or equal to the
  // initial indent, then it's a new token. We can't encode that in CsLex, so we have to match
  // INDENT tokens and single line tokens and fix it up in post production.
  // (We also have to handle the continuation line starting with any of the chars allowed in
  // `ns-plain-safe` that isn't allowed in `ns-plain-first`. I.e. `c-indicator` but not
  // `c-flow-indicator`)
  public partial class YamlLexerGenerated
  {
    private readonly bool myAllowChameleonOptimizations;

    // ReSharper disable InconsistentNaming
    private TokenNodeType currentTokenType;
    private bool explicitKey = false;

    private int lastNewLineOffset;
    public int currentLineIndent;
    
    public YamlLexerGenerated(IBuffer buffer, bool allowChameleonOptimizations = true) : this(buffer)
    {
      myAllowChameleonOptimizations = allowChameleonOptimizations;
    }
    

    // The indent of the indicator of a block scalar. The contents must be more
    // indented than this. However, we also need to handle the case where the
    // indicator is at column 0, but we're in `block-in` context, i.e. at the
    // root of the document. This allows the indent to also be at column 0 (the
    // parent node is treated as being at column -1)
    // TODO: Use the indentation indicator value to set this

    private int parentNodeIndent;
    
    // The number of unclosed LBRACE and LBRACK. flowLevel == 0 means block context
    private int flowLevel;
    
    protected bool AtChameleonStart = false;
    protected int InitialLexicalState = YYINITIAL;

    public struct TokenPosition
    {
      public TokenNodeType CurrentTokenType;
      public int LastNewLineOffset;
      public int CurrentLineIndent;
      public int ParentNodeIndent;
      public int FlowLevel;
      public int YyBufferIndex;
      public int YyBufferStart;
      public int YyBufferEnd;
      public int YyLexicalState;
      public bool AtChameleonStart;
    }

    public virtual void Start()
    {
      Start(0, yy_buffer.Length, (uint)InitialLexicalState);
    }

    public void Start(int startOffset, int endOffset, uint state)
    {
      yy_buffer_index = yy_buffer_start = yy_buffer_end = startOffset;
      yy_eof_pos = endOffset;
      yy_lexical_state = (int) state;
      currentTokenType = null;
    }

    public void Advance()
    {
      LocateToken();
      currentTokenType = null;
    }

    public object CurrentPosition
    {
      get
      {
        TokenPosition tokenPosition;
        tokenPosition.CurrentTokenType = currentTokenType;
        tokenPosition.LastNewLineOffset = lastNewLineOffset;
        tokenPosition.CurrentLineIndent = currentLineIndent;
        tokenPosition.ParentNodeIndent = parentNodeIndent;
        tokenPosition.FlowLevel = flowLevel;
        tokenPosition.YyBufferIndex = yy_buffer_index;
        tokenPosition.YyBufferStart = yy_buffer_start;
        tokenPosition.YyBufferEnd = yy_buffer_end;
        tokenPosition.YyLexicalState = yy_lexical_state;
        tokenPosition.AtChameleonStart = AtChameleonStart;
        return tokenPosition;
      }
      set
      {
        var tokenPosition = (TokenPosition) value;
        currentTokenType = tokenPosition.CurrentTokenType;
        lastNewLineOffset = tokenPosition.LastNewLineOffset;
        currentLineIndent = tokenPosition.CurrentLineIndent;
        parentNodeIndent = tokenPosition.ParentNodeIndent;
        flowLevel = tokenPosition.FlowLevel;
        yy_buffer_index = tokenPosition.YyBufferIndex;
        yy_buffer_start = tokenPosition.YyBufferStart;
        yy_buffer_end = tokenPosition.YyBufferEnd;
        yy_lexical_state = tokenPosition.YyLexicalState;
        AtChameleonStart = tokenPosition.AtChameleonStart;
      }
    }

    public TokenNodeType TokenType => LocateToken();

    public int TokenStart
    {
      get
      {
        LocateToken();
        return yy_buffer_start;
      }
    }

    public int TokenEnd
    {
      get
      {
        LocateToken();
        return yy_buffer_end;
      }
    }

    public IBuffer Buffer => yy_buffer;
    public uint LexerStateEx => (uint) yy_lexical_state;

    public int EOFPos => yy_eof_pos;
    public int LexemIndent => 7;  // No, I don't know why

    private bool IsBlock => flowLevel == 0;

    private TokenNodeType LocateToken()
    {
      if (currentTokenType == null)
      {
        try
        {
          currentTokenType = _locateToken();
          AtChameleonStart = false;
        }
        catch (Exception e)
        {
          e.AddData("TokenType", () => currentTokenType);
          e.AddData("LexerState", () => LexerStateEx);
          e.AddData("TokenStart", () => yy_buffer_start);
          e.AddData("TokenPos", () => yy_buffer_index);
          e.AddData("Buffer", () =>
          {
            var start = Math.Max(0, yy_buffer_end);
            var tokenText = yy_buffer.GetText(new TextRange(start, yy_buffer_index));
            tokenText = Regex.Replace(tokenText, @"\p{Cc}", a => $"[{(byte) a.Value[0]:X2}]");
            return tokenText;
          });
          throw;
        }
      }

      return currentTokenType;
    }

    private TokenNodeType TryEatLinesWithGreaterIndent()
    {
      var previousLastNewLineOffset = lastNewLineOffset;
      var previousLineIndent = currentLineIndent;
      var count = EatLinesWithGreaterIndent();
      if (count == 1)
      {
        RewindToken();
        currentLineIndent = previousLineIndent;
        lastNewLineOffset = previousLastNewLineOffset;
        return _locateToken();
      }
      else
      {
        yy_at_bol = true;
        return YamlTokenType.GetChameleonMapEntryValueWithIndent(previousLineIndent); 
      }
    }

    private int EatLinesWithGreaterIndent()
    {
      var linesCount = 0;
      RewindChar();
      var curIndent = currentLineIndent;
      
      EatToEnd();
      linesCount++;
      lastNewLineOffset = yy_buffer_index;
      
      while (IsIndentGreaterOrEmptyLine(curIndent) || yy_buffer_index != yy_eof_pos && Buffer[yy_buffer_index] == '#')
      {
        linesCount++;
        EatToEnd();
        lastNewLineOffset = yy_buffer_index;
      }

      yy_buffer_index = lastNewLineOffset;
      yy_buffer_end = lastNewLineOffset;
      currentLineIndent = 0;

      yybegin(BLOCK);

      return linesCount;
    }

    private void EatToEnd()
    {
      while (true)
      {
        if (yy_buffer_index == yy_eof_pos)
          break;

        var ch = Buffer[yy_buffer_index];
        if (ch == '\n')
        {
          yy_buffer_index++;
          break;
        } 
        
        if (ch == '\r')
        {
          yy_buffer_index++;

          if (yy_buffer_index < yy_eof_pos && Buffer[yy_buffer_index] == '\n')
            yy_buffer_index++;

          break;
        }

        yy_buffer_index++;
      }
    }

    private bool IsIndentGreaterOrEmptyLine(int parentIndent)
    {
      currentLineIndent = 0;
      while (true)
      {
        if (yy_buffer_index == yy_eof_pos)
          break;

        if (Buffer[yy_buffer_index] == ' ')
        {
          currentLineIndent++;
        }
        else
        {
          break;
        }

        yy_buffer_index++;
        
      }

      if (yy_buffer_index + 2 < yy_eof_pos)
      {
        if (Buffer[yy_buffer_index] == '-' && Buffer[yy_buffer_index  + 1] == ' ')
        {
          currentLineIndent += 2;
        }
      }

      // hack for multiline strings with empty lines
      // TODO 2019.3 krasnotsvetov RIDER-31051
      if (currentLineIndent == 0)
      {
        if (yy_buffer_index == yy_eof_pos)
          return false;
        
        var curChar = Buffer[yy_buffer_index];
        if (curChar == '\r' || curChar == '\n' || curChar == '\'' || curChar == '"')
          return true;

      }
      
      return currentLineIndent > parentIndent;
    }

    private void HandleIndent()
    {
      if (yy_buffer_index < yy_eof_pos && Buffer[yy_buffer_index] == ' ')
        currentLineIndent += 2;
    }
    
    
    // For completeness: These rewind functions don't reset any current indents or last new line offset. As long as you
    // don't rewind multiple times, especially across a new line or an indent, this shouldn't cause an issue
    private void RewindToken()
    {
      yy_buffer_end = yy_buffer_index = yy_buffer_start;
    }

    private void RewindChar()
    {
      yy_buffer_end = yy_buffer_index = yy_buffer_index - 1;
    }

    private void RewindWhitespace()
    {
      while (yy_buffer_index > 0 && IsWhitespace(yy_buffer[yy_buffer_index - 1]))
        yy_buffer_end = yy_buffer_index = yy_buffer_index - 1;
    }

    private static bool IsWhitespace(char c) => c == ' ' || c == '\t';

    private void PushFlowIndicator()
    {
      flowLevel++;
      parentNodeIndent = yy_buffer_start - lastNewLineOffset;
      yybegin(FLOW);
    }

    private void PopFlowIndicator()
    {
      flowLevel = Math.Max(0, flowLevel - 1);
      if (IsBlock)
        yybegin(BLOCK);
    }

    private void ResetBlockFlowState()
    {
      yybegin(IsBlock ? BLOCK : FLOW);
    }

    private void HandleNewLine(bool resetParentNodeIndent = true)
    {
      currentLineIndent = 0;
      if (resetParentNodeIndent)
        parentNodeIndent = -1;
      lastNewLineOffset = yy_buffer_end;
    }

    private TokenNodeType HandleImplicitKey()
    {
      parentNodeIndent = yy_buffer_start - lastNewLineOffset;
      // The lexer matches an implicit key as a single line ns-plain, followed by optional whitespace and a colon
      // We want the lexer to return this as separate tokens (which the parser will then match as an implicit key), so
      // rewind to the end of the ns-plain and let the next lexer advance match the whitespace and colon.
      RewindChar();
      RewindWhitespace();
      return YamlTokenType.NS_PLAIN_ONE_LINE_IN;
    }

    private void HandleExplicitKeyIndicator()
    {
      explicitKey = true;
      parentNodeIndent = yy_buffer_start - lastNewLineOffset;
    }

    private void HandleSequenceItemIndicator()
    {
      parentNodeIndent = yy_buffer_start - lastNewLineOffset;
    }

    private void BeginBlockScalar()
    {
      // Note that block scalar indent is based on the parent node, not the indent
      // of the indicator
      yybegin(BLOCK_SCALAR_HEADER);
    }

    private void HandleBlockScalarWhitespace()
    {
      if (currentLineIndent <= parentNodeIndent)
        ResetBlockFlowState();
    }

    private TokenNodeType HandleBlockScalarLine()
    {
      if (currentLineIndent <= parentNodeIndent)
      {
        EndBlockScalar();
        RewindToken();
        return _locateToken();
      }

      return YamlTokenType.SCALAR_TEXT;
    }

    private void EndBlockScalar()
    {
      ResetBlockFlowState();
    }

    private void BeginJsonAdjacentValue()
    {
      yybegin(JSON_ADJACENT_VALUE);
    }

    private void EndJsonAdjacentValue()
    {
      ResetBlockFlowState();
    }

    private TokenNodeType AbandonJsonAdjacentValueState()
    {
      RewindToken();
      ResetBlockFlowState();
      return _locateToken();
    }
  }
}