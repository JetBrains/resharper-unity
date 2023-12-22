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
                if (it is not IShaderLabTokenNodeType { IsKeyword: true } tt)
                    continue;
                var keywordType = tt.GetKeywordType(errorElement);
                // only offer ShaderLab command keywords if they are first on line, they are still valid for parser, but looks bad for code style
                // exclusion for ITexturePropertyValue block to let it one-line
                if (isFirstOnLine || errorElement.Parent is ITexturePropertyValue || !keywordType.IsCommandKeyword())
                    result.Add(CreateKeywordCompletionResult(tt, keywordType));
            }
            return result.ReadOnlyList();
        }

        public void Suggest(ShaderLabCodeCompletionContext context, IItemsCollector collector)
        {
            var psiIconManager = context.BasicContext.PsiIconManager;
            var keywordsCollector = new KeywordsCollector(collector, psiIconManager.KeywordIcon, (ulong)(ShaderLabLookupItemRelevance.Keywords | ShaderLabLookupItemRelevance.LiveTemplates));
            
            var keywords = GetKeywords(context);
            foreach (var keyword in keywords)
                keywordsCollector.Add(keyword.Token, context.CompletionRanges, keyword.TailType);
        }

        private KeywordCompletionResult CreateKeywordCompletionResult(IShaderLabTokenNodeType nodeType, ShaderLabKeywordType keywordType)
        {
            var tailType = keywordType switch
            {
                ShaderLabKeywordType.RegularCommand => ShaderLabTailType.Space,
                ShaderLabKeywordType.BlockCommand when nodeType == ShaderLabTokenType.SET_TEXTURE_KEYWORD => ShaderLabTailType.Brackets,
                ShaderLabKeywordType.BlockCommand when nodeType == ShaderLabTokenType.SHADER_KEYWORD => ShaderLabTailType.Space,
                ShaderLabKeywordType.BlockCommand => ShaderLabTailType.Braces,
                _ => TailType.None
            };
            return new KeywordCompletionResult(nodeType, tailType);
        }
        
        struct KeywordCompletionResult
        {
            public ITokenNodeType Token { get; }
            public TailType TailType { get; }

            public KeywordCompletionResult(ITokenNodeType token, TailType tailType)
            {
                Token = token;
                TailType = tailType;
            }
        }
    }
}