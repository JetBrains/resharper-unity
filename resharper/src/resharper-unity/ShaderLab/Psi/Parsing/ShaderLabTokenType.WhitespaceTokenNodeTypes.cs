using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing
{
    public partial class ShaderLabTokenType
    {
        private sealed class WhitespaceNodeType : ShaderLabTokenNodeType
        {
            public WhitespaceNodeType(int index)
                : base("WHITESPACE", index)
            {
            }

            public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
            {
                return new Whitespace(buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
            }

            public override LeafElementBase Create(string token)
            {
                return new Whitespace(token);
            }

            public override bool IsWhitespace => true;
            public override string TokenRepresentation => " ";
        }

        private sealed class NewLineNodeType : ShaderLabTokenNodeType
        {
            public NewLineNodeType(int index)
                : base("NEW_LINE", index)
            {
            }

            public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
            {
                return new NewLine(buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
            }

            public override LeafElementBase Create(string token)
            {
                return new NewLine(token);
            }

            public override bool IsWhitespace => true;
            public override string TokenRepresentation => @"\r\n";
        }
    }
}