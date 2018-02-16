using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Feature.Services.CodeCompletion
{
    public class ShaderLabCodeCompletionContext : SpecificCodeCompletionContext
    {
        public ShaderLabCodeCompletionContext([NotNull] CodeCompletionContext context,
                                              ShaderLabReparsedCompletionContext unterminatedContext,
                                              TextLookupRanges ranges)
            : base(context)
        {
            UnterminatedContext = unterminatedContext;
            Ranges = ranges;
        }

        public override string ContextId => "ShaderLabCodeCompletionContext";

        public ShaderLabReparsedCompletionContext UnterminatedContext { get; }
        public TextLookupRanges Ranges { get; }
    }
}