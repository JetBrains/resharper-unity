using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing
{
    public partial class ShaderLabTokenType
    {
        private sealed class EndOfLineCommentNodeType : ShaderLabTokenNodeType
        {
            public EndOfLineCommentNodeType(int index)
                : base("END_OF_LINE_COMMENT", index)
            {
            }

            public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
            {
                return new EndOfLineComment(buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
            }

            public override LeafElementBase Create(string token)
            {
                return new EndOfLineComment(token);
            }

            public override bool IsComment => true;
            public override string TokenRepresentation => "// comment";
        }

        private sealed class MultiLineCommentNodeType : ShaderLabTokenNodeType
        {
            public MultiLineCommentNodeType(int index)
                : base("MULTI_LINE_COMMENT", index)
            {
            }

            public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
            {
                return new MultiLineComment(buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
            }

            public override LeafElementBase Create(string token)
            {
                return new MultiLineComment(token);
            }

            public override bool IsComment => true;
            public override string TokenRepresentation => "/* comment */";
        }
    }
}