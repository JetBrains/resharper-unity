using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing
{
    internal class CgFilteringLexer : FilteringLexer, ILexer<int>
    {
        private readonly CgPreProcessor myPreProcessor;

        public CgFilteringLexer([NotNull] ILexer lexer, [CanBeNull] CgPreProcessor preProcessor)
            : base(lexer)
        {
            myPreProcessor = preProcessor;
        }

        protected override bool Skip(TokenNodeType tokenType)
        {
            if (tokenType.IsWhitespace || tokenType.IsComment || tokenType.IsFiltered)
                return true;

            return myPreProcessor != null && myPreProcessor.IsInPpTokenRange(TokenStart);
        }

        int ILexer<int>.CurrentPosition
        {
            get => ((ILexer<int>) myLexer).CurrentPosition;
            set => ((ILexer<int>) myLexer).CurrentPosition = value;
        }
    }
}