using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing
{
    public partial class ShaderLabTokenType
    {
        // A fixed length token node type, e.g. keyword, symbol, etc.
        private class FixedTokenNodeType : ShaderLabTokenNodeType, IFixedTokenNodeType
        {
            public FixedTokenNodeType(string s, int index, string representation)
                : base(s, index)
            {
                TokenRepresentation = representation;
            }
            
            public LeafElementBase Create() => new FixedTokenElement(this);

            public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset) => Create();

            public override string TokenRepresentation { get; }
        }

        // An instance of a fixed length token node type that will be added to the PSI tree
        private abstract class FixedTokenElementBase : ShaderLabTokenBase
        {
        }

        private class FixedTokenElement : FixedTokenElementBase
        {
            private readonly FixedTokenNodeType myTokenNodeType;

            public FixedTokenElement(FixedTokenNodeType tokenNodeType)
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