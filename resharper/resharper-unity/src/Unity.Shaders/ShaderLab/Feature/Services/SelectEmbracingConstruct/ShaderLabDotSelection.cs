#nullable enable
using JetBrains.ReSharper.Feature.Services.SelectEmbracingConstruct;
using JetBrains.ReSharper.Plugins.Unity.Common.Psi.Syntax;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Syntax;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.SelectEmbracingConstruct
{
    class ShaderLabDotSelection : DotSelection<ShaderLabFile>
    {
        private static CLikeSyntax Syntax => ShaderLabSyntax.CLike;
        
        public ShaderLabDotSelection(ShaderLabFile fileNode, TreeOffset offset, bool selectBetterToken, bool useCamelHumps, bool appendInjectedPsi) : base(fileNode, offset, selectBetterToken, useCamelHumps, appendInjectedPsi)
        {
        }

        protected override ISelectedRange? GetParentInternal(ITokenNode tokenNode)
        {
            if (Syntax.STRING_LITERALS.Contains(tokenNode.NodeType))
                return new ShaderLabTokenPartSelection(FileNode, tokenNode.GetUnquotedTreeTextRange(), tokenNode);
            return null;
        }

        protected override ISelectedRange CreateTokenPartSelection(ITokenNode tokenNode, TreeTextRange treeTextRange) => new ShaderLabTokenPartSelection(FileNode, treeTextRange, tokenNode);

        protected override ISelectedRange CreateTreeNodeSelection(ITokenNode tokenNode) => new ShaderLabTreeNodeSelection(FileNode, tokenNode, ExtendToTheWholeLinePolicy.DO_NOT_EXTEND);

        protected override bool IsWordToken(ITokenNode token) => token.GetTokenType() is { IsKeyword: true } or { IsIdentifier: true };

        protected override bool IsLiteralToken(ITokenNode token) => Syntax.STRING_LITERALS.Contains(token.NodeType);

        protected override bool IsSpaceToken(ITokenNode token) => token.NodeType == Syntax.NEW_LINE || token.NodeType == Syntax.WHITE_SPACE;

        protected override bool IsNewLineToken(ITokenNode token) => token.NodeType == Syntax.NEW_LINE;
    }
}