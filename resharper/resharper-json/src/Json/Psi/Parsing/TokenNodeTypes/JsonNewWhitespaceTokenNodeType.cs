using JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodeTypes
{
    internal class JsonNewWhitespaceTokenNodeType : JsonNewTokenNodeTypeBase
    {
        public JsonNewWhitespaceTokenNodeType(int index)
            : base("WHITESPACE", index)
        {
        }

        public override LeafElementBase Create(string token)
        {
            return new JsonNewWhitespaceTokenNode(token);
        }

        public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
        {
            return new JsonNewWhitespaceTokenNode(buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
        }

        public override string TokenRepresentation => " ";

        public override bool IsWhitespace => true;
    }
}