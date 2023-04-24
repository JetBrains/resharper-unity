using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Psi.ExpectedTypes;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.UI.Icons;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.CodeCompletion.Keywords
{
    public readonly struct KeywordsCollector
    {
        private readonly IItemsCollector myCollector;
        private readonly IconId myKeywordIcon;
        private readonly ulong myKeywordRelevance;

        public KeywordsCollector(IItemsCollector collector, IconId keywordIcon, ulong keywordRelevance)
        {
            myCollector = collector;
            myKeywordIcon = keywordIcon;
            myKeywordRelevance = keywordRelevance;
        }

        public void Add(TokenNodeType keyword, TextLookupRanges completionRanges, TailType tailType)
        {
            var text = keyword.TokenRepresentation;
            var textLookupRanges = completionRanges;
            var info = new TextualInfo(text, text)
            {
                Ranges = textLookupRanges,
                TailType = tailType
            };

            info.Placement.Relevance |= myKeywordRelevance;

            var icon = myKeywordIcon;
            var item = LookupItemFactory.CreateLookupItem(info)
                .WithPresentation(item => new TextPresentation<TextualInfo>(item.Info, icon, emphasize: true))
                .WithBehavior(static item => new TextualBehavior<TextualInfo>(item.Info))
                .WithMatcher(static item => new TextualMatcher<TextualInfo>(item.Info));

            item.PutKey(CompletionKeys.IsKeywordKey);
            myCollector.Add(item);
        }
    }
}