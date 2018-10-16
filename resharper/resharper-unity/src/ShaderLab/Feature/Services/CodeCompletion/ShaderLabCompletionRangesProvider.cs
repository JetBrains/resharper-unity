using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Feature.Services.CodeCompletion
{
    [Language(typeof(ShaderLabLanguage))]
    public class ShaderLabCompletionRangesProvider : ItemsProviderOfSpecificContext<ShaderLabCodeCompletionContext>,
        ICompletionRangesProvider
    {
        protected override TextLookupRanges GetDefaultRanges(ShaderLabCodeCompletionContext context)
        {
            return context.Ranges;
        }
    }
}