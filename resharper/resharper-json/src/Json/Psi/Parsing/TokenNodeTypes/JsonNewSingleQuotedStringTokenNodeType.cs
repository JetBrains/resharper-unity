using JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodeTypes
{
    internal class JsonNewSingleQuotedStringTokenNodeType : JsonNewTokenNodeTypeBase
    {
        public JsonNewSingleQuotedStringTokenNodeType(int index)
            : base("SINGLE_QUOTED_STRING", index)
        {
        }

        public override string TokenRepresentation => "'single quoted string'";

        public override bool IsConstantLiteral => true;
        public override bool IsStringLiteral => true;

        public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
        {
            return new JsonNewGenericTokenNode(this, buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
        }

        public override LeafElementBase Create(string token)
        {
            return new JsonNewGenericTokenNode(this, token);
        }
    }
}