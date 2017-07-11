#if RIDER

using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Host.Features.Foldings;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Parsing;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Host.Features.Foldings.ShaderLab
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

        public override void VisitBlockValueNode(IBlockValue blockValue, IHighlightingConsumer consumer)
        {
            var containingNode = blockValue.GetContainingNode<IBlockCommand>();
            Assertion.AssertNotNull(containingNode, "containingNode != null");
            if (containingNode != null)
                consumer.AddFoldingForBracedConstruct(blockValue.LBrace, blockValue.RBrace, containingNode);
        }

        public override void VisitGrabPassValueNode(IGrabPassValue grabPassValue, IHighlightingConsumer consumer)
        {
            var containingNode = grabPassValue.GetContainingNode<IGrabPassDef>();
            Assertion.AssertNotNull(containingNode, "containingNode != null");
            if (containingNode != null)
                consumer.AddFoldingForBracedConstruct(grabPassValue.LBrace, grabPassValue.RBrace, containingNode);
        }

        public override void VisitRegularPassValueNode(IRegularPassValue regularPassValue, IHighlightingConsumer consumer)
        {
            var containingNode = regularPassValue.GetContainingNode<IRegularPassDef>();
            Assertion.AssertNotNull(containingNode, "containingNode != null");
            if (containingNode != null)
                consumer.AddFoldingForBracedConstruct(regularPassValue.LBrace, regularPassValue.RBrace, containingNode);
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