#nullable enable

using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.CodeCompletion.Keywords;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Formatting;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.CodeCompletion
{
    [Language(typeof(ShaderLabLanguage))]
    public class ShaderLabKeywordCompletionRule : ItemsProviderOfSpecificContext<ShaderLabCodeCompletionContext>
    {
        private readonly ShaderLabKeywordSuggester myKeywordSuggester;

        public ShaderLabKeywordCompletionRule(ShaderLabCodeFormatter codeFormatter)
        {
            myKeywordSuggester = new(codeFormatter);
        }

        protected override bool IsAvailable(ShaderLabCodeCompletionContext context) => context.BasicContext.CodeCompletionType == CodeCompletionType.BasicCompletion;
        
        protected override bool AddLookupItems(ShaderLabCodeCompletionContext context, IItemsCollector collector)
        {
            myKeywordSuggester.Suggest(context, collector);
            return true;
        }
    }
}