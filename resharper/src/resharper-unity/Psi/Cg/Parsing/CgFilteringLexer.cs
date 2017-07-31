using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Cg
{
    public class CgFilteringLexer : FilteringLexer, ILexer<int>
    {
        public CgFilteringLexer([NotNull] ILexer lexer)
            : base(lexer)
        {
        }

        protected override bool Skip(TokenNodeType tokenType)
        {
            return tokenType.IsWhitespace || tokenType.IsComment;
        }

        int ILexer<int>.CurrentPosition
        {
            get => ((ILexer<int>) myLexer).CurrentPosition;
            set => ((ILexer<int>) myLexer).CurrentPosition = value;
        }
    }
}