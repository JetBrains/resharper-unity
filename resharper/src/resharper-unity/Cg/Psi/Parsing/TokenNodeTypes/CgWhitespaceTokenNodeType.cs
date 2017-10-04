using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodeTypes
{
    internal class CgWhitespaceTokenNodeType : CgTokenNodeTypeBase
    {
        public CgWhitespaceTokenNodeType(int index)
            : base("WHITESPACE", index)
        {
        }

        public override LeafElementBase Create(string token)
        {
            return new CgWhitespaceTokenNode(token);
        }

        public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
        {
            return new CgWhitespaceTokenNode(buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
        }

        public override string TokenRepresentation => " ";

        public override bool IsWhitespace => true;
    }
}