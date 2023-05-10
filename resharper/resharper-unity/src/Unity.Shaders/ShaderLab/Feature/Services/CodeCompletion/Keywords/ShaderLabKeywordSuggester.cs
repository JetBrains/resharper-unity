#nullable enable

using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Psi.ExpectedTypes;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.CodeCompletion.Keywords
{
    public class ShaderLabKeywordSuggester
    {
        private IEnumerable<KeywordCompletionResult> GetKeywords(ShaderLabCodeCompletionContext context)
        {
            var treeNode = context.UnterminatedContext.TreeNode;
            if (treeNode is { Parent: UnexpectedTokenErrorElement { ExpectedTokenTypes: { } expectedTokenTypes } })
                return expectedTokenTypes.Where(it => it is ITokenNodeType { IsKeyword: true }).Select(it => new KeywordCompletionResult((ITokenNodeType)it));
            
            return EmptyList<KeywordCompletionResult>.Instance;
        }

        public void Suggest(ShaderLabCodeCompletionContext context, IItemsCollector collector)
        {
            var psiIconManager = context.BasicContext.PsiIconManager;
            var keywordsCollector = new KeywordsCollector(collector, psiIconManager.KeywordIcon, (ulong)(ShaderLabLookupItemRelevance.Keywords | ShaderLabLookupItemRelevance.LiveTemplates));
            
            var keywords = GetKeywords(context);
            foreach (var keyword in keywords)
                keywordsCollector.Add(keyword.Token, context.CompletionRanges, TailType.None);
        }
        
        struct KeywordCompletionResult
        {
            public ITokenNodeType Token { get; }
            public TailType TailType { get; }

            public KeywordCompletionResult(ITokenNodeType token) : this(token, TailType.None) { }
            
            public KeywordCompletionResult(ITokenNodeType token, TailType tailType)
            {
                Token = token;
                TailType = tailType;
            }
        }
    }
}