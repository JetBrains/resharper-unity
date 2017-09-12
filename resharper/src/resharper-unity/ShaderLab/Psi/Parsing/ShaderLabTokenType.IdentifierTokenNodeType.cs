using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing
{
    public static partial class ShaderLabTokenType
    {
        private sealed class IdentifierNodeType : ShaderLabTokenNodeType
        {
            public IdentifierNodeType(int index)
                : base("IDENTIFIER", index)
            {
            }

            public override string TokenRepresentation => "identifier01";

            public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
            {
                return new Identifier(buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
            }

            public override LeafElementBase Create(string token)
            {
                return new Identifier(token);
            }
        }
    }
}