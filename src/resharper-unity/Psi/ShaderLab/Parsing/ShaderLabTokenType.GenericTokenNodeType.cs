using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Parsing
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
                return new GenericTokenElement(this);
            }

            public override string TokenRepresentation { get; }
        }

        // An instance of a fixed length token node type that will be added to the PSI tree
        private class GenericTokenElement : ShaderLabTokenBase
        {
            private readonly GenericTokenNodeType myTokenNodeType;

            public GenericTokenElement(GenericTokenNodeType tokenNodeType)
            {
                myTokenNodeType = tokenNodeType;
            }

            public override NodeType NodeType => myTokenNodeType;

            public override int GetTextLength()
            {
                return myTokenNodeType.TokenRepresentation.Length;
            }

            public override string GetText()
            {
                return myTokenNodeType.TokenRepresentation;
            }
        }

    }
}