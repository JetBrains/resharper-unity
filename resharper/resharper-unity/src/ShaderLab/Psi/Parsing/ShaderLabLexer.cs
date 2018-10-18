using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing
{
    public class ShaderLabLexer : ShaderLabLexerGenerated
    {
        public ShaderLabLexer(IBuffer buffer)
            : base(buffer)
        {
        }

        public ShaderLabLexer(IBuffer buffer, int startOffset, int endOffset)
            : base(buffer, startOffset, endOffset)
        {
        }

        public override TokenNodeType _locateToken()
        {
            var token = base._locateToken();

            // The "unquoted string literal" inside a property attribute is both
            // delimited by parens, and can contain them, mismatched. We can't do
            // that solely with regex in the .lex file. Note that we can only get
            // this while in PARENS lexical state, so we have had an LPAREN
            if (token == ShaderLabTokenType.UNQUOTED_STRING_LITERAL)
            {
                var unquotedStringLiteralStart = BufferStart;
                var lastNonWhitespaceCookie = LexerStateCookie.Create(this);
                LexerStateCookie.Common? beforeLastRParenCookie = null;
                LexerStateCookie.Common? beforeCommaCookie = null;

                do
                {
                    // Get the next token. This will update TokenStart and TokenEnd
                    var nextToken = base._locateToken();

                    // BAD_CHARACTER is a hard delimiter for unquoted string literal.
                    // Roll back, avoiding trailing whitespace
                    if (nextToken == ShaderLabTokenType.BAD_CHARACTER)
                    {
                        lastNonWhitespaceCookie.Dispose();
                        BufferStart = unquotedStringLiteralStart;
                        return token;
                    }

                    // COMMA should be a hard delimiter, but it messes up checking for the
                    // last RPAREN. E.g. `[Header(something), foo]` should roll back to
                    // beforeLastRParenCookie, while `[Header(something, whatever)]` should
                    // roll back to beforeCommaCookie
                    if (nextToken == ShaderLabTokenType.COMMA)
                    {
                        if (beforeCommaCookie.HasValue)
                        {
                            beforeCommaCookie.Value.Dispose();
                            BufferStart = unquotedStringLiteralStart;
                            return token;
                        }
                        beforeCommaCookie = lastNonWhitespaceCookie;
                    }

                    // This RPAREN might be part of the unquoted string literal, or it
                    // might be the last RPAREN in the attribute. If we've seen a comma
                    // before this, this RPAREN is likely the end of the attribute, but
                    // the unquoted string literal ends at the COMMA, so roll back there
                    if (nextToken == ShaderLabTokenType.RPAREN)
                    {
                        if (beforeCommaCookie.HasValue)
                        {
                            beforeCommaCookie.Value.Dispose();
                            BufferStart = unquotedStringLiteralStart;
                            return token;
                        }

                        beforeLastRParenCookie = lastNonWhitespaceCookie;
                    }

                    // Found the end of the attribute, roll back to the last RPAREN if
                    // there is one, else to the last non-whitespace token. Also, switch
                    // to BRACKETS (we're not consuming RBRACK, or whatever followed the
                    // final RPAREN right now)
                    if (nextToken == ShaderLabTokenType.RBRACK
                        || nextToken == ShaderLabTokenType.NEW_LINE)
                    {
                        if (beforeLastRParenCookie.HasValue)
                        {
                            beforeLastRParenCookie.Value.Dispose();
                            SetState(PARENS);
                        }
                        else
                            lastNonWhitespaceCookie.Dispose();
                        BufferStart = unquotedStringLiteralStart;
                        return token;
                    }

                    // Track the last bit of non-whitespace
                    if (nextToken != ShaderLabTokenType.WHITESPACE)
                        lastNonWhitespaceCookie = LexerStateCookie.Create(this);

                } while (BufferEnd < EOFPos);
            }

            return token;
        }
    }
}