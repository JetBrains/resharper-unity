using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Gen;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Parsing
{
    internal class ShaderLabParser : ShaderLabParserGenerated, IParser
    {
        private ITokenIntern myTokenIntern;

        public ShaderLabParser([NotNull] ILexer<int> lexer)
        {
            setLexer(new ShaderLabFilteringLexer(lexer));
        }

        public ITokenIntern TokenIntern => myTokenIntern ?? (myTokenIntern = new LexerTokenIntern(10));

        public IFile ParseFile()
        {
            var element = ParseShaderLabFile();
            return (IFile)element;
        }

        protected override TreeElement createToken()
        {
            var tokenType = myLexer.TokenType;

            Assertion.Assert(tokenType != null, "tokenType != null");

            LeafElementBase element;
            if (tokenType == ShaderLabTokenType.NUMERIC_LITERAL
                || tokenType == ShaderLabTokenType.STRING_LITERAL
                || tokenType == ShaderLabTokenType.CG_CONTENT)
            {
                var text = TokenIntern.Intern(myLexer);
                element = new ShaderLabTokenType.GenericTokenElement(tokenType, text);
            }
            else
                element = tokenType.Create(myLexer.Buffer, new TreeOffset(myLexer.TokenStart),
                    new TreeOffset(myLexer.TokenEnd));

            myLexer.Advance();

            return element;
        }

        public override TreeElement ParseErrorElement()
        {
            // NOTE: Doesn't Advance
            var result = TreeElementFactory.CreateErrorElement(ParserMessages.GetUnexpectedTokenMessage());
            return result;
        }

        public override TreeElement ParseErrorElementWithoutRBrace()
        {
            return ParseErrorElement();
        }

        private bool IsIdentifier(TokenNodeType token)
        {
            // TODO: This should just be a bitset
            // At the very least, it needs to be a better check
            return token == ShaderLabTokenType.IDENTIFIER || token.IsKeyword;
        }

        public override TreeElement ParseErrorPropertyValue()
        {
            var result = TreeElementFactory.CreateErrorElement(ParserMessages.GetUnexpectedTokenMessage());

            // Consume until we hit either an identifier (start of another property)
            // an LBRACK (start of attributes for another property) or RBRACE
            while (!IsIdentifier(myLexer.TokenType)
                && myLexer.TokenType != ShaderLabTokenType.LBRACK
                && myLexer.TokenType != ShaderLabTokenType.RBRACE)
            {
                if (myLexer.TokenType == ShaderLabTokenType.LBRACE)
                    SkipNestedBraces(result);
                else
                    skip(result);
            }
            return result;
        }

        private void SkipNestedBraces(CompositeElement result)
        {
            while (myLexer.TokenType != ShaderLabTokenType.RBRACE)
            {
                skip(result);
                if (myLexer.TokenType == ShaderLabTokenType.LBRACE)
                    SkipNestedBraces(result);
            }

            // Skip the final RBRACE
            skip(result);
        }

        public override void ParseErrorTexturePropertyBlockValues(CompositeElement result)
        {
            // Parse anything with the `{ }` of a texture property block
            if (myLexer.TokenType == ShaderLabTokenType.RBRACE)
                return;

            var errorElement = TreeElementFactory.CreateErrorElement(ParserMessages.GetUnexpectedTokenMessage());
            while (myLexer.TokenType != ShaderLabTokenType.RBRACE)
                skip(errorElement);
            result.AppendNewChild(errorElement);
        }
    }
}