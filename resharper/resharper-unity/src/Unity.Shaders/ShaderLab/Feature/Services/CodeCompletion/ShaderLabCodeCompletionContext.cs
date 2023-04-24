using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Cpp.CodeCompletion;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.CodeCompletion
{
    public class ShaderLabCodeCompletionContext : SpecificCodeCompletionContext, ISpecificCodeCompletionContextWithRanges
    {
        public ShaderLabCodeCompletionContext([NotNull] CodeCompletionContext context,
                                              ShaderLabReparsedCompletionContext unterminatedContext,
                                              TextLookupRanges completionRanges)
            : base(context)
        {
            UnterminatedContext = unterminatedContext;
            CompletionRanges = completionRanges;
        }

        public override string ContextId => "ShaderLabCodeCompletionContext";

        public ShaderLabReparsedCompletionContext UnterminatedContext { get; }
        public TextLookupRanges CompletionRanges { get; }
    }
}