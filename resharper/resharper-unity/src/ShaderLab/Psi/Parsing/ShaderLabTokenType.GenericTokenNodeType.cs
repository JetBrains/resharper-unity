using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing
{
    public partial class ShaderLabTokenType
    {
        // A generic length token node type, e.g. string literals, identifiers etc.
        private class GenericTokenNodeType : ShaderLabTokenNodeType
        {
            public GenericTokenNodeType(string s, int index, string representation)
                : base(s, index)
            {
                TokenRepresentation = representation;
            }

            public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
            {
                return new GenericTokenElement(this, buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
            }

            public override string TokenRepresentation { get; }
        }

        private class FilteredGenericTokenNodeType : GenericTokenNodeType
        {
            public FilteredGenericTokenNodeType(string s, int index, string representation)
                : base(s, index, representation)
            {
            }

            public override bool IsFiltered => true;
        }

        // An instance of a fixed length token node type that will be added to the PSI tree
        public class GenericTokenElement : ShaderLabTokenBase
        {
            private readonly TokenNodeType myTokenNodeType;
            private readonly string myText;

            public GenericTokenElement(TokenNodeType tokenNodeType, string text)
            {
                myTokenNodeType = tokenNodeType;
                myText = text;
            }

            public override NodeType NodeType => myTokenNodeType;
            public override int GetTextLength() => myText.Length;
            public override string GetText() => myText;
        }
    }
}