#nullable enable
using JetBrains.ReSharper.Feature.Services.SelectEmbracingConstruct;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.SelectEmbracingConstruct
{
    class ShaderLabTreeNodeSelection : TreeNodeSelection<ShaderLabFile>
    {
        public ShaderLabTreeNodeSelection(ShaderLabFile fileNode, ITreeNode node, ExtendToTheWholeLinePolicy extendToWholeLine = ExtendToTheWholeLinePolicy.EXTEND_IF_ALL_SPACES_AROUND) : base(fileNode, node)
        {
            ExtendToWholeLine = extendToWholeLine;
        }

        public override ISelectedRange? Parent
        {
            get
            {
                foreach (var containingNode in TreeNode.ContainingNodes())
                {
                    if (containingNode is IBlockValue blockValue && ShaderLabBlockValueSelection.TryCreate(FileNode, blockValue, Range) is {} blockValueSelection)
                        return blockValueSelection; 
                    if (containingNode is IShaderLabCommand or IPropertyDeclaration)
                        return new ShaderLabTreeNodeSelection(FileNode, containingNode);
                }

                return null;
            }
        }

        public override ExtendToTheWholeLinePolicy ExtendToWholeLine { get; }
    }
}