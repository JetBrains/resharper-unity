using JetBrains.Diagnostics;
using JetBrains.ReSharper.Daemon.CodeFolding;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.ShaderLab.Daemon.CodeFolding
{
    internal class ShaderLabCodeFoldingProcessor : TreeNodeVisitor<FoldingHighlightingConsumer>, ICodeFoldingProcessor
    {
        public bool InteriorShouldBeProcessed(ITreeNode element, FoldingHighlightingConsumer consumer) => true;
        public bool IsProcessingFinished(FoldingHighlightingConsumer consumer) => false;

        public void ProcessBeforeInterior(ITreeNode element, FoldingHighlightingConsumer consumer)
        {
            var treeNode = element as IShaderLabTreeNode;
            treeNode?.Accept(this, consumer);
        }

        public void ProcessAfterInterior(ITreeNode element, FoldingHighlightingConsumer consumer)
        {
        }

        public override void VisitNode(ITreeNode node, FoldingHighlightingConsumer consumer)
        {
            consumer.AddFoldingForCLikeCommentTokens(node, ShaderLabTokenType.END_OF_LINE_COMMENT,
                ShaderLabTokenType.MULTI_LINE_COMMENT, ShaderLabTokenType.NEW_LINE);
        }

#pragma warning disable 672
        // Obsolete warning tells us to also implement VisitTexturePropertyValueNode
        public override void VisitBlockValueNode(IBlockValue blockValue, FoldingHighlightingConsumer consumer)
#pragma warning restore 672
        {
            var containingNode = blockValue.GetContainingNode<IBlockCommand>()
                .NotNull("blockValue.GetContainingNode<IBlockCommand>() != null");
            consumer.AddFoldingForBracedConstruct(blockValue.LBrace, blockValue.RBrace, containingNode);
        }

        public override void VisitTexturePropertyValueNode(ITexturePropertyValue texturePropertyValue, FoldingHighlightingConsumer consumer)
        {
#pragma warning disable 618
            VisitBlockValueNode(texturePropertyValue, consumer);
#pragma warning restore 618
        }

        public override void VisitCgContentNode(ICgContent cgContent, FoldingHighlightingConsumer consumer)
        {
            var range = cgContent.GetHighlightingRange();
            if (!range.IsEmpty)
                consumer.AddDefaultPriorityFolding(CodeFoldingAttributes.DEFAULT_FOLDING_ATTRIBUTE, range, "{...}");
        }
    }
}