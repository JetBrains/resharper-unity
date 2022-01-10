using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing
{
    public static partial class ShaderLabTokenType
    {
        private sealed class StringLiteralNodeType : ShaderLabTokenNodeType
        {
            public StringLiteralNodeType(int index)
                : base("STRING_LITERAL", index)
            {
            }

            public override string TokenRepresentation => "\"XXX\"";

            public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
            {
                return new GenericTokenElement(this, buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
            }

            public override LeafElementBase Create(string token)
            {
                return new GenericTokenElement(this, token);
            }

            public override bool IsStringLiteral => true;
        }

        private sealed class UnquotedStringLiteralNodeType : ShaderLabTokenNodeType
        {
            public UnquotedStringLiteralNodeType(int index)
                : base("UNQUOTED_STRING_LITERAL", index)
            {
            }

            public override string TokenRepresentation => "string value";

            public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
            {
                return new GenericTokenElement(this, buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
            }

            public override LeafElementBase Create(string token)
            {
                return new GenericTokenElement(this, token);
            }

            public override bool IsStringLiteral => true;
        }
    }
}
