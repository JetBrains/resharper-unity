using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Parsing
{
    public class ShaderLabFilteringLexer : FilteringLexer, ILexer<int>
    {
        public ShaderLabFilteringLexer(ILexer lexer)
            : base(lexer)
        {
        }

        protected override bool Skip(TokenNodeType tokenType)
        {
            return tokenType.IsWhitespace || tokenType.IsComment;
        }

        int ILexer<int>.CurrentPosition
        {
            get { return ((ILexer<int>) myLexer).CurrentPosition; }
            set { ((ILexer<int>) myLexer).CurrentPosition = value; }
        }
    }
}