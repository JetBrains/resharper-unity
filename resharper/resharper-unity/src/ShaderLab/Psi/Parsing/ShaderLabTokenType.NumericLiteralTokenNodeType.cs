using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing
{
    public static partial class ShaderLabTokenType
    {
        private sealed class NumericLiteralNodeType : ShaderLabTokenNodeType
        {
            public NumericLiteralNodeType(int index)
                : base("NUMERIC_LITERAL", index)
            {
            }

            public override string TokenRepresentation => "0.0";

            public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
            {
                return new GenericTokenElement(this, buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
            }

            public override LeafElementBase Create(string token)
            {
                return new GenericTokenElement(this, token);
            }

            public override bool IsConstantLiteral => true;
        }
    }
}
