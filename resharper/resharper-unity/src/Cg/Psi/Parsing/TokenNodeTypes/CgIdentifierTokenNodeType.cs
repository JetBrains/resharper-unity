using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodes;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodeTypes
{
    internal class CgIdentifierTokenNodeType : CgTokenNodeTypeBase
    {
        public CgIdentifierTokenNodeType(int index)
            : base("IDENTIFIER", index)
        {
        }

        public override LeafElementBase Create(string token)
        {
            return new CgIdentifierTokenNode(token);
        }

        public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
        {
            return new CgIdentifierTokenNode(buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
        }

        public override string TokenRepresentation => "identifier";
    }
}