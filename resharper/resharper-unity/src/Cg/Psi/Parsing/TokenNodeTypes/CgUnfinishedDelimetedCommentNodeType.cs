using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodeTypes
{
    internal class CgUnfinishedDelimetedCommentNodeType : CgTokenNodeTypeBase
    {
        public CgUnfinishedDelimetedCommentNodeType(int index)
            : base("UNFINISHED_DELIMITED_COMMENT", index)
        {
        }

        public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
        {
            return new CgUnfinishedDelimitedCommentNode(buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
        }

        public override LeafElementBase Create(string token)
        {
            return new CgUnfinishedDelimitedCommentNode(token);
        }

        public override bool IsComment => true;
        public override string TokenRepresentation => "/* comment";
    }
}