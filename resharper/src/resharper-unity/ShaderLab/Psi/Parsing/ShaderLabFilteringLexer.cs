using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing
{
    internal class ShaderLabFilteringLexer : FilteringLexer, ILexer<int>
    {
        [CanBeNull]
        private readonly ShaderLabPreProcessor myPreProcessor;

        public ShaderLabFilteringLexer(ILexer lexer, [CanBeNull] ShaderLabPreProcessor preProcessor)
            : base(lexer)
        {
            myPreProcessor = preProcessor;
        }

        protected override bool Skip(TokenNodeType tokenType)
        {
            if (tokenType.IsWhitespace || tokenType.IsComment || tokenType.IsFiltered)
                return true;

            return myPreProcessor != null && myPreProcessor.IsInPPTokenRange(TokenStart);
        }

        int ILexer<int>.CurrentPosition
        {
            get => ((ILexer<int>) myLexer).CurrentPosition;
            set => ((ILexer<int>) myLexer).CurrentPosition = value;
        }
    }
}