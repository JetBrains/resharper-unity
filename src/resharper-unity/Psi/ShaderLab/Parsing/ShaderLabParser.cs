using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Gen;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Parsing
{
    internal class ShaderLabParser : ShaderLabParserGenerated, IParser
    {
        public ShaderLabParser([NotNull] ILexer<int> lexer)
        {
            setLexer(new ShaderLabFilteringLexer(lexer));
        }

        public IFile ParseFile()
        {
            var element = ParseShaderLabFile();
            // TODO: Insert filtered tokens
            return (IFile) element;
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