using JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodeTypes
{
    internal class JsonNewIdentifierTokenNodeType : JsonNewTokenNodeTypeBase
    {
        public JsonNewIdentifierTokenNodeType(int index)
            : base("IDENTIFIER", index)
        {
        }

        public override LeafElementBase Create(string token)
        {
            return new JsonNewIdentifierTokenNode(token);
        }

        public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
        {
            return new JsonNewIdentifierTokenNode(buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
        }

        public override string TokenRepresentation => "identifier";
        public override bool IsIdentifier => true;
    }
}