using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Gen;
using JetBrains.ReSharper.Psi;
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

        protected override TreeElement matchPropertiesCommand()
        {
            return matchNamedIdentifier("Properties");
        }

        protected override bool expectPropertiesCommand()
        {
            return expectNamedIdentifier("Properties");
        }

        private TreeElement matchNamedIdentifier(string name)
        {
            if (expectNamedIdentifier(name))
                return CreateToken(ShaderLabTokenType.IDENTIFIER);
            throw new UnexpectedToken(ParserMessages.GetUnexpectedTokenMessage());
        }

        private bool expectNamedIdentifier(string name)
        {
            var tokenType = myLexer.TokenType;
            return tokenType == ShaderLabTokenType.IDENTIFIER && LexerUtil.CompareTokenText(myLexer, name, false);
        }

        private TreeElement CreateToken(TokenNodeType tokenType)
        {
            var element = tokenType.Create(myLexer.Buffer, new TreeOffset(myLexer.TokenStart),
                new TreeOffset(myLexer.TokenEnd));
            SetOffset(element, myLexer.TokenStart);
            myLexer.Advance();
            return element;
        }
    }
}