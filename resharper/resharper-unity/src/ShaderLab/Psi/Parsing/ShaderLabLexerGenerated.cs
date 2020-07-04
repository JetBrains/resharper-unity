﻿using System;
using System.Text.RegularExpressions;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing
{
    public partial class ShaderLabLexerGenerated
    {
      private readonly Func<IBuffer, ILexer> myCppLexerFactory = null;
      private static readonly LexerDictionary<TokenNodeType> Keywords = new LexerDictionary<TokenNodeType>(false);

         private readonly ReusableBufferRange myBufferRange = new ReusableBufferRange();
        // ReSharper disable once InconsistentNaming
        private TokenNodeType currentTokenType;

        private struct TokenPosition
        {
            public TokenNodeType CurrentTokenType;
            public int YyBufferIndex;
            public int YyBufferStart;
            public int YyBufferEnd;
            public int YyLexicalState;
        }

        static ShaderLabLexerGenerated()
        {
            foreach (var nodeType in ShaderLabTokenType.KEYWORDS)
            {
                var keyword = (TokenNodeType) nodeType;
                Keywords[keyword.TokenRepresentation] = keyword;
            }
        }
        
        public ShaderLabLexerGenerated(IBuffer buffer, Func<IBuffer,ILexer> cppLexerFactory) : this(buffer)
        {
          myCppLexerFactory = cppLexerFactory;
        }

        public void Start()
        {
            Start(0, yy_buffer.Length, YYINITIAL);
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
                tokenPosition.YyBufferIndex = yy_buffer_index;
                tokenPosition.YyBufferStart = yy_buffer_start;
                tokenPosition.YyBufferEnd = yy_buffer_end;
                tokenPosition.YyLexicalState = yy_lexical_state;
                return tokenPosition;
            }
            set
            {
                var tokenPosition = (TokenPosition) value;
                currentTokenType = tokenPosition.CurrentTokenType;
                yy_buffer_index = tokenPosition.YyBufferIndex;
                yy_buffer_start = tokenPosition.YyBufferStart;
                yy_buffer_end = tokenPosition.YyBufferEnd;
                yy_lexical_state = tokenPosition.YyLexicalState;
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

        protected int BufferStart
        {
            get => yy_buffer_start;
            set => yy_buffer_start = value;
        }

        protected int BufferEnd
        {
            get => yy_buffer_end;
            set => yy_buffer_end = value;
        }

        protected void SetState(int lexerState)
        {
            yy_lexical_state = lexerState;
        }

        public int EOFPos => yy_eof_pos;
        public int LexemIndent => 7;    // No, I don't know why
        public uint LexerStateEx => (uint) yy_lexical_state;


        private ILexer myActiveCppLexer = null;
        private int LastCGContentStart = -1;
        private int LastCGContentEnd = -1;

        private void PropagateChangesFromCppLexer()
        {
          Assertion.Assert(myActiveCppLexer != null, "myActiveCppLexer != null");
          yy_buffer_start = myActiveCppLexer.TokenStart + LastCGContentStart;
          yy_buffer_end = myActiveCppLexer.TokenEnd + LastCGContentStart;
          yy_buffer_index = myActiveCppLexer.TokenStart + LastCGContentStart;
          currentTokenType = myActiveCppLexer.TokenType;  
        }

        private TokenNodeType LocateToken()
        {
            if (currentTokenType == null)
            {
                try
                {
                    if (myActiveCppLexer != null)
                    {
                      myActiveCppLexer.Advance();
                      PropagateChangesFromCppLexer();
                      if (currentTokenType == null)
                      {
                        myActiveCppLexer = null;
                        Assertion.Assert(yy_buffer_start == LastCGContentEnd, "yy_buffer_start == LastCGContentEnd");
                        Assertion.Assert(yy_buffer_end == LastCGContentEnd, "yy_buffer_start == LastCGContentEnd");
                        Assertion.Assert(yy_buffer_index == LastCGContentEnd, "yy_buffer_start == LastCGContentEnd");
                      }
                      else
                        return currentTokenType;
                    }
                    currentTokenType = _locateToken();

                    if (currentTokenType == ShaderLabTokenType.CG_CONTENT && myCppLexerFactory != null)
                    {
                        myActiveCppLexer = myCppLexerFactory(ProjectedBuffer.Create(yy_buffer, new TextRange(TokenStart, TokenEnd)));
                        myActiveCppLexer.Start();
                        LastCGContentStart = TokenStart;
                        LastCGContentEnd = TokenEnd;
                        PropagateChangesFromCppLexer();
                        
                        Assertion.Assert(currentTokenType != null, "cppLexer.TokenType != null");
                        return currentTokenType;
                    }
                    
                    if (currentTokenType == ShaderLabTokenType.UNQUOTED_STRING_LITERAL)
                    {
                        while (yy_buffer_index < yy_eof_pos)
                        {
                            if (Buffer[yy_buffer_index] == ')' && !IsBracketAfterWhiteSpaces(yy_buffer_index + 1))
                            {
                                yy_buffer_end++;
                                yy_buffer_index++;

                                MoveToNextBracket();
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
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
                        tokenText = Regex.Replace(tokenText, @"\p{Cc}", a => string.Format("[{0:X2}]", (byte)a.Value[0]));
                        return tokenText;
                    });
                    throw;
                }
            }

            return currentTokenType;
        }

        private void MoveToNextBracket()
        {
            var curIndex = yy_buffer_index;
            var hasAnyChar = false;
            while (yy_buffer_index < yy_eof_pos)
            {
                var curChar = Buffer[curIndex];
                if (curChar == ']')
                    break;
                
                if (curChar == ',')
                    break;
                
                if (curChar == '\n')
                    break;
                
                if (curChar == '\r')
                    break;

                if (curChar == ')')
                    break;

                hasAnyChar |= !char.IsWhiteSpace(curChar);
                
                curIndex++;
            }

            if (hasAnyChar)
            {
                yy_buffer_end = yy_buffer_index = curIndex;
            }

        }

        private bool IsBracketAfterWhiteSpaces(int curPos)
        {
            while (curPos < yy_eof_pos)
            {
                if (Buffer[curPos] == ']')
                    return true;

                if (!char.IsWhiteSpace(Buffer[curPos]))
                    return false;
                
                curPos++;
            }
            return false;
        }

        private TokenNodeType FindKeywordByCurrentToken()
        {
            return Keywords.GetValueSafe(myBufferRange, yy_buffer, yy_buffer_start, yy_buffer_end);
        }

        private TokenNodeType HandleNestedMultiLineComment()
        {
            var depth = 1;
            while (depth > 0)
            {
                var c = yy_buffer[yy_buffer_index++];
                if (yy_buffer_index >= yy_eof_pos)
                    break;
                var c2 = yy_buffer[yy_buffer_index];
                if (c == '*' && c2 == '/')
                    depth--;
                if (c == '/' && c2 == '*')
                    depth++;
            }
            yy_buffer_index++;
            if (yy_buffer_index > yy_eof_pos)
                yy_buffer_index = yy_eof_pos;
            yy_buffer_end = yy_buffer_index;

            return ShaderLabTokenType.MULTI_LINE_COMMENT;
        }
    }
}