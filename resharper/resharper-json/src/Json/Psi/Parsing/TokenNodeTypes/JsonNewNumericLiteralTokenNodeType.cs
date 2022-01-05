using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Parsing.TokenNodes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Parsing.TokenNodeTypes
{
    internal class JsonNewNumericLiteralTokenNodeType : JsonNewTokenNodeTypeBase
    {
        public JsonNewNumericLiteralTokenNodeType(int index)
            : base("NUMERIC_LITERAL", index)
        {
        }

        public override string TokenRepresentation => "0";

        public override bool IsConstantLiteral => true;
        
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