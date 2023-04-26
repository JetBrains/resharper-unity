#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi.ExpectedTypes;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.CodeCompletion.Keywords
{
    public class ShaderLabKeywordSuggester
    {
        readonly Dictionary<object, List<(TokenNodeType Token, bool Exclusive)>> myScopedSuggesters = new();

        public ShaderLabKeywordSuggester()
        {
            // top-level
            AddScopedKeyword(typeof(ShaderLabFile), ShaderLabTokenType.SHADER_KEYWORD, true);
            // shader value
            AddScopedKeyword(typeof(ShaderValue), ShaderLabTokenType.PROPERTIES_KEYWORD, true);
            AddScopedKeyword(typeof(ShaderValue), ShaderLabTokenType.CATEGORY_KEYWORD, false);
            AddScopedKeyword(typeof(ShaderValue), ShaderLabTokenType.SUB_SHADER_KEYWORD, false);
            AddScopedKeyword(typeof(ShaderValue), ShaderLabTokenType.FALLBACK_KEYWORD, false);
            AddScopedKeyword(typeof(ShaderValue), ShaderLabTokenType.DEPENDENCY_KEYWORD, false);
            AddScopedKeyword(typeof(ShaderValue), ShaderLabTokenType.CUSTOM_EDITOR_KEYWORD, false);
            // category value
            AddStateKeywords(typeof(CategoryValue));
        }

        /// <summary>Adds state keywords (tags, render state, legacy render state).</summary>
        /// <param name="scopeType">Type of scope supporting state keywords.</param>
        private void AddStateKeywords(Type scopeType)
        {
            AddScopedKeyword(scopeType, ShaderLabTokenType.TAGS_KEYWORD, false);
            AddScopedKeyword(scopeType, ShaderLabTokenType.ZWRITE_KEYWORD, false);
            AddScopedKeyword(scopeType, ShaderLabTokenType.CONSERVATIVE_KEYWORD, false);
            AddScopedKeyword(scopeType, ShaderLabTokenType.ZCLIP_KEYWORD, false);
            AddScopedKeyword(scopeType, ShaderLabTokenType.OFFSET_KEYWORD, false);
            AddScopedKeyword(scopeType, ShaderLabTokenType.COLOR_MASK_KEYWORD, false);
            AddScopedKeyword(scopeType, ShaderLabTokenType.BLEND_KEYWORD, false);
            AddScopedKeyword(scopeType, ShaderLabTokenType.BLEND_OP_KEYWORD, false);
            AddScopedKeyword(scopeType, ShaderLabTokenType.ALPHA_TO_MASK_KEYWORD, false);
            AddScopedKeyword(scopeType, ShaderLabTokenType.STENCIL_KEYWORD, false);
            AddScopedKeyword(scopeType, ShaderLabTokenType.NAME_KEYWORD, false);
            AddScopedKeyword(scopeType, ShaderLabTokenType.LOD_KEYWORD, false);
        }

        private void AddScopedKeyword(Type scopeType, TokenNodeType keyword, bool exclusive)
        {
            myScopedSuggesters.GetOrCreateValue(scopeType, () => new()).Add((keyword, exclusive));
        }

        private static object? GetErrorScope(IErrorElement error)
        {
            if (error.FindPrevSibling(static it => it.NodeType == ShaderLabTokenType.RBRACE) is not null)
                return FindContainingScope(error);
            var errorOwner = error.Parent;
            return errorOwner switch
            {
                IBlockCommand blockCommand when blockCommand.FirstChild == error                    => FindContainingScope(blockCommand),  // first token in block command is a keyword itself, provide it's scope for keywords suggestion
                IBlockValue   blockValue   when error.IsNodeWithinBraces()                          => blockValue.GetType(),               // when error is inside block value it handles errors only within parenthesis and should advice all keywords valid for the block body
                _ => null
            };
        }

        private static object FindContainingScope(ITreeNode node) => node.FindParent(static node => node is IBlockValue)?.GetType() ?? typeof(ShaderLabFile);

        private static object? GetScope(ITreeNode? treeNode)
        {
            return treeNode switch
            {
                null => null,
                {Parent: IErrorElement errorElement} => GetErrorScope(errorElement),
                _ => treeNode.GetType()
            };
        }

        private IEnumerable<KeywordCompletionResult> GetKeywords(ShaderLabCodeCompletionContext context)
        {
            var treeNode = context.UnterminatedContext.TreeNode;
            if (treeNode is { Parent: UnexpectedTokenErrorElement { ExpectedTokenTypes: { } expectedTokenTypes } })
                return expectedTokenTypes.Where(it => it is TokenNodeType { IsKeyword: true }).Select(it => new KeywordCompletionResult((TokenNodeType)it));
                
            var scope = GetScope(treeNode);
            if (scope != null && myScopedSuggesters.TryGetValue(scope, out var keywords))
                return keywords.Select(it => new KeywordCompletionResult(it.Token));
            return Array.Empty<KeywordCompletionResult>();
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
            public TokenNodeType Token { get; }
            public TailType TailType { get; }

            public KeywordCompletionResult(TokenNodeType token) : this(token, TailType.None) { }
            
            public KeywordCompletionResult(TokenNodeType token, TailType tailType)
            {
                Token = token;
                TailType = tailType;
            }
        }
    }
}