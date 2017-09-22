#if RIDER

using JetBrains.ReSharper.Daemon.CodeFolding;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Host.Features.Foldings
{
    internal class ShaderLabCodeFoldingProcessor : TreeNodeVisitor<IHighlightingConsumer>, ICodeFoldingProcessor
    {
        public bool InteriorShouldBeProcessed(ITreeNode element, IHighlightingConsumer consumer) => true;
        public bool IsProcessingFinished(IHighlightingConsumer consumer) => false;

        public void ProcessBeforeInterior(ITreeNode element, IHighlightingConsumer consumer)
        {
            var treeNode = element as IShaderLabTreeNode;
            treeNode?.Accept(this, consumer);
        }

        public void ProcessAfterInterior(ITreeNode element, IHighlightingConsumer consumer)
        {
        }

        public override void VisitNode(ITreeNode node, IHighlightingConsumer consumer)
        {
            consumer.AddFoldingForCLikeCommentTokens(node, ShaderLabTokenType.END_OF_LINE_COMMENT,
                ShaderLabTokenType.MULTI_LINE_COMMENT, ShaderLabTokenType.NEW_LINE);
        }

#pragma warning disable 672
        // Obsolete warning tells us to also implement VisitTexturePropertyValueNode
        public override void VisitBlockValueNode(IBlockValue blockValue, IHighlightingConsumer consumer)
#pragma warning restore 672
        {
            var containingNode = blockValue.GetContainingNode<IBlockCommand>();
            Assertion.AssertNotNull(containingNode, "containingNode != null");
            if (containingNode != null)
                consumer.AddFoldingForBracedConstruct(blockValue.LBrace, blockValue.RBrace, containingNode);
        }

        public override void VisitTexturePropertyValueNode(ITexturePropertyValue texturePropertyValue, IHighlightingConsumer consumer)
        {
            VisitBlockValueNode(texturePropertyValue, consumer);
        }

        public override void VisitCgContentNode(ICgContent cgContent, IHighlightingConsumer consumer)
        {
            var range = cgContent.GetHighlightingRange();
            if (range.IsNotEmptyNormalized())
            {
                // TODO: Might be nice to have a better repreentation
                // This will fold to `CGPROGRAM{...}ENDCG`
                // But what? `{ CGPROGRAM }`?
                consumer.AddDefaultPriorityFolding(CodeFoldingAttributes.DEFAULT_FOLDING_ATTRIBUTE, range, "{...}");
            }
        }
    }
}

#endif