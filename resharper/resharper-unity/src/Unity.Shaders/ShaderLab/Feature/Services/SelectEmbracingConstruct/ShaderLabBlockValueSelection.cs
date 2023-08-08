#nullable enable
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.SelectEmbracingConstruct;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.SelectEmbracingConstruct
{
    class ShaderLabBlockValueSelection : SelectedRangeBase<ShaderLabFile>
    {
        private readonly IBlockValue myBlockValue;

        public static ShaderLabBlockValueSelection? TryCreate(ShaderLabFile fileNode, IBlockValue blockValue, DocumentRange range)
        {
            var treeRange = fileNode.Translate(range);
            if (blockValue is { LBrace: { } lBrace, RBrace: { } rBrace } && treeRange.StartOffset >= lBrace.GetTreeEndOffset() && treeRange.EndOffset < rBrace.GetTreeStartOffset())
            {
                var startToken = lBrace.GetNextNonWhitespaceToken();
                if (startToken == null || startToken.GetTreeStartOffset() > treeRange.StartOffset)
                    return null;
                var endToken = rBrace.GetPreviousNonWhitespaceToken();
                if (endToken == null || endToken.GetTreeEndOffset() < treeRange.EndOffset)
                    return null;
                return new ShaderLabBlockValueSelection(fileNode, blockValue, fileNode.GetDocumentRange(new TreeTextRange(startToken.GetTreeStartOffset(), endToken.GetTreeEndOffset())));
            }
            return null;
        }
            
        private ShaderLabBlockValueSelection(ShaderLabFile fileNode, IBlockValue blockValue, DocumentRange documentRange) : base(fileNode, documentRange)
        {
            myBlockValue = blockValue;
        }

        public override ISelectedRange? Parent
        {
            get
            {
                var parentNode = myBlockValue.GetContainingNode<IShaderLabCommand>();
                return parentNode != null ? new ShaderLabTreeNodeSelection(FileNode, parentNode) : null;
            }
        }
    }
}