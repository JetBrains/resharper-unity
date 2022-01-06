using JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodeTypes
{
    internal class JsonNewLineCommentTokenNodeType : JsonNewTokenNodeTypeBase
    {
        public JsonNewLineCommentTokenNodeType(int index)
            : base("SINGLE_LINE_COMMENT", index)
        {
        }

        public override LeafElementBase Create(string token)
        {
            return new JsonNewLineCommentNode(token);
        }

        public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
        {
            return new JsonNewLineCommentNode(buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
        }

        public override bool IsComment => true;

        public override string TokenRepresentation => "// single line comment";
    }
}