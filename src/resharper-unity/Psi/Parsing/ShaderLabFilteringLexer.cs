using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Parsing
{
    public class ShaderLabFilteringLexer : FilteringLexer
    {
        public ShaderLabFilteringLexer(ILexer lexer)
            : base(lexer)
        {
        }

        protected override bool Skip(TokenNodeType tokenType)
        {
            return tokenType.IsWhitespace || tokenType.IsComment;
        }
    }
}