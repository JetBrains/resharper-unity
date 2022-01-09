using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodeTypes
{
    internal class CgFixedLengthTokenNodeType : CgTokenNodeTypeBase
    {
        public CgFixedLengthTokenNodeType(string s, int index, string representation)
            : base(s, index)
        {
            TokenRepresentation = representation;
        }

        public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
        {
            return new CgFixedLengthTokenNode(this);
        }

        public override string TokenRepresentation { get; }
    }
}