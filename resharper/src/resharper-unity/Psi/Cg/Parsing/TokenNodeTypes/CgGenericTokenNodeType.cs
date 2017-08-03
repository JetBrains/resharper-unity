using JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Parsing.TokenNodes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Parsing.TokenNodeTypes
{
    internal class CgGenericTokenNodeType : CgTokenNodeTypeBase
    {
        public CgGenericTokenNodeType(string s, int index, string representation)
            : base(s, index)
        {
            TokenRepresentation = representation;
        }

        public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
        {
            return new CgGenericTokenNode(this, buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
        }

        public override string TokenRepresentation { get; }
    }
}