using System;
using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Daemon.CaretDependentFeatures;
using JetBrains.ReSharper.Feature.Services.Contexts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.MatchingBrace;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.UI.RichText;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.ContextHighlighters
{
    // Note that the ContainingBracesContextHighlighterBase base class is for matching
    // tokens that are children of a single element
    [ContainsContextConsumer]
    public class ShaderLabMatchingBraceContextHighlighter : MatchingBraceContextHighlighterBase<ShaderLabLanguage>
    {
        [CanBeNull, AsyncContextConsumer]
        public static Action ProcessDataContext(
            [NotNull] Lifetime lifetime,
            [NotNull, ContextKey(typeof(ContextHighlighterPsiFileView.ContextKey))]
           IPsiDocumentRangeView psiDocumentRangeView,
            [NotNull] InvisibleBraceHintManager invisibleBraceHintManager,
            [NotNull] MatchingBraceSuggester matchingBraceSuggester,
            [NotNull] MatchingBraceConsumerFactory consumerFactory,
            [NotNull] HighlightingProlongedLifetime prolongedLifetime)
        {
            var highlighter = new ShaderLabMatchingBraceContextHighlighter();
            return highlighter.ProcessDataContextImpl(lifetime, prolongedLifetime, psiDocumentRangeView,
                    invisibleBraceHintManager, matchingBraceSuggester, consumerFactory);
        }

        protected override void TryHighlightToLeft(MatchingHighlightingsConsumer consumer, ITokenNode selectedToken, TreeOffset treeOffset)
        {
            var selectedTokenType = selectedToken.GetTokenType();

            if (IsRightBracket(selectedTokenType))
            {
                if (FindMatchingLeftBracket(selectedToken, out var matchedNode))
                {
                    var singleChar = IsSingleCharToken(matchedNode);
                    consumer.ConsumeMatchedBraces(matchedNode.GetDocumentRange(), selectedToken.GetDocumentRange(), singleChar);
                    consumer.ConsumeInvisibleBraceHint(new InvisibleBraceHint(
                        matchedNode.GetDocumentRange(), selectedToken.GetDocumentRange(), textControl => GetHintText(textControl, matchedNode)));
                }
                else
                {
                    consumer.ConsumeHighlighting(HighlightingAttributeIds.UNMATCHED_BRACE, selectedToken.GetDocumentEndOffset().ExtendLeft(1));

                    if (matchedNode != null)
                        consumer.ConsumeHighlighting(HighlightingAttributeIds.UNMATCHED_BRACE, matchedNode.GetDocumentStartOffset().ExtendRight(1));
                }
            }
            else if (selectedTokenType == ShaderLabTokenType.STRING_LITERAL)
            {
                if (treeOffset == selectedToken.GetTreeTextRange().EndOffset)
                {
                    consumer.ConsumeMatchedBraces(
                        selectedToken.GetDocumentStartOffset().ExtendRight(1), selectedToken.GetDocumentEndOffset().ExtendLeft(1));
                }
            }
        }

        protected override void TryHighlightToRight(MatchingHighlightingsConsumer consumer, ITokenNode selectedToken, TreeOffset treeOffset)
        {
            var selectedTokenType = selectedToken.GetTokenType();

            if (IsLeftBracket(selectedTokenType))
            {
                if (FindMatchingRightBracket(selectedToken, out var matchedNode))
                {
                    var singleChar = IsSingleCharToken(matchedNode);
                    consumer.ConsumeMatchedBraces(selectedToken.GetDocumentRange(), matchedNode.GetDocumentRange(), singleChar);
                }
                else
                {
                    consumer.ConsumeHighlighting(HighlightingAttributeIds.UNMATCHED_BRACE, selectedToken.GetDocumentStartOffset().ExtendRight(1));

                    if (matchedNode != null)
                        consumer.ConsumeHighlighting(HighlightingAttributeIds.UNMATCHED_BRACE, matchedNode.GetDocumentEndOffset().ExtendLeft(1));
                }
            }
            else if (selectedTokenType == ShaderLabTokenType.STRING_LITERAL)
            {
                if (treeOffset == selectedToken.GetTreeTextRange().StartOffset)
                {
                    consumer.ConsumeMatchedBraces(
                        selectedToken.GetDocumentStartOffset().ExtendRight(1), selectedToken.GetDocumentEndOffset().ExtendLeft(1));
                }
            }
        }

        protected override bool IsLeftBracket(TokenNodeType tokenType)
        {
            return tokenType == ShaderLabTokenType.LBRACE
                   || tokenType == ShaderLabTokenType.LBRACK
                   || tokenType == ShaderLabTokenType.LPAREN
                   || tokenType == ShaderLabTokenType.CG_INCLUDE
                   || tokenType == ShaderLabTokenType.CG_PROGRAM;
        }

        protected override bool IsRightBracket(TokenNodeType tokenType)
        {
            return tokenType == ShaderLabTokenType.RBRACE
                   || tokenType == ShaderLabTokenType.RBRACK
                   || tokenType == ShaderLabTokenType.RPAREN
                   || tokenType == ShaderLabTokenType.CG_END;
        }

        protected override bool Match(TokenNodeType token1, TokenNodeType token2)
        {
            if (token1 == ShaderLabTokenType.LBRACE) return token2 == ShaderLabTokenType.RBRACE;
            if (token1 == ShaderLabTokenType.RBRACE) return token2 == ShaderLabTokenType.LBRACE;
            if (token1 == ShaderLabTokenType.LBRACK) return token2 == ShaderLabTokenType.RBRACK;
            if (token1 == ShaderLabTokenType.RBRACK) return token2 == ShaderLabTokenType.LBRACK;
            if (token1 == ShaderLabTokenType.LPAREN) return token2 == ShaderLabTokenType.RPAREN;
            if (token1 == ShaderLabTokenType.RPAREN) return token2 == ShaderLabTokenType.LPAREN;
            if (token1 == ShaderLabTokenType.CG_INCLUDE) return token2 == ShaderLabTokenType.CG_END;
            if (token1 == ShaderLabTokenType.CG_PROGRAM) return token2 == ShaderLabTokenType.CG_END;
            if (token1 == ShaderLabTokenType.CG_END)
                return token2 == ShaderLabTokenType.CG_INCLUDE || token2 == ShaderLabTokenType.CG_PROGRAM;

            return false;
        }

        private static bool IsSingleCharToken(ITreeNode token)
        {
            var tokenType = token.NodeType;
            return tokenType != ShaderLabTokenType.CG_END
                   && tokenType != ShaderLabTokenType.CG_INCLUDE
                   && tokenType != ShaderLabTokenType.CG_PROGRAM;
        }

        private RichTextBlock GetHintText(ITextControl textControl, ITreeNode lBraceNode)
        {
            if (lBraceNode == null || lBraceNode.GetTokenType() != ShaderLabTokenType.LBRACE)
                return null;

            var parent = lBraceNode.Parent as IBlockValue;
            var blockCommand = parent?.Parent as IBlockCommand;
            return blockCommand != null ? MatchingBraceUtil.PrepareRichText(textControl, blockCommand, lBraceNode, true) : null;
        }
    }
}
