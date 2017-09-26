using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodeTypes
{
    internal class CgNewLineTokenNodeType : CgTokenNodeTypeBase
    {
        public CgNewLineTokenNodeType(int index)
            : base("NEW_LINE", index)
        {
        }

        public override LeafElementBase Create(string token)
        {
            return new CgNewLineTokenNode(token);
        }

        public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
        {
            return new CgNewLineTokenNode(buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
        }

        public override bool IsWhitespace => true;
        public override string TokenRepresentation => @"\r\n";
    }
}