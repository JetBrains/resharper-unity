using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Parsing.TokenNodeTypes
{
    internal class CgWhitespaceTokenNodeType : CgTokenNodeTypeBase
    {
        public CgWhitespaceTokenNodeType(int index)
            : base("WHITESPACE", index)
        {
        }

        public override LeafElementBase Create(string token)
        {
            throw new System.NotImplementedException();
        }

        public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
        {
            throw new System.NotImplementedException();
        }

        public override string TokenRepresentation => " ";

        public override bool IsWhitespace => true;
    }
}