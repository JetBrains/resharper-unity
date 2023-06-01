#nullable enable

using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Formatting;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi.ExpectedTypes;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Impl.CodeStyle;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.CodeCompletion.Keywords
{
    public class ShaderLabKeywordSuggester
    {
        private readonly ShaderLabCodeFormatter myCodeFormatter;

        public ShaderLabKeywordSuggester(ShaderLabCodeFormatter codeFormatter)
        {
            myCodeFormatter = codeFormatter;
        }

        private IReadOnlyList<KeywordCompletionResult> GetKeywords(ShaderLabCodeCompletionContext context)
        {
            var treeNode = context.UnterminatedContext.TreeNode;
            if (treeNode is not { Parent: UnexpectedTokenErrorElement { ExpectedTokenTypes: { } expectedNodeTypes } errorElement })
                return EmptyList<KeywordCompletionResult>.Instance;
            
            var isFirstOnLine = treeNode.IsFirstOnLine(myCodeFormatter);
            var result = new LocalList<KeywordCompletionResult>();
            foreach (var it in expectedNodeTypes)
            {
                if (
                    it is IShaderLabTokenNodeType { IsKeyword: true } tt
                    // only offer ShaderLab command keywords if they are first on line, they are still valid for parser, but looks bad for code style
                    // exclusion for ITexturePropertyValue block to let it one-line
                    && (isFirstOnLine || errorElement.Parent is ITexturePropertyValue || !tt.IsCommandKeyword(errorElement))  
                )
                    result.Add(new KeywordCompletionResult(tt));
            }
            return result.ReadOnlyList();
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