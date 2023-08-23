#nullable enable
using JetBrains.ReSharper.Feature.Services.SelectEmbracingConstruct;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.SelectEmbracingConstruct
{
    class ShaderLabTokenPartSelection : TokenPartSelection<ShaderLabFile>
    {
        public ShaderLabTokenPartSelection(ShaderLabFile fileNode, TreeTextRange treeTextRange, ITokenNode token) : base(fileNode, treeTextRange, token)
        {
        }

        public override ISelectedRange Parent => new ShaderLabTreeNodeSelection(FileNode, Token, ExtendToTheWholeLinePolicy.DO_NOT_EXTEND);
    }
}