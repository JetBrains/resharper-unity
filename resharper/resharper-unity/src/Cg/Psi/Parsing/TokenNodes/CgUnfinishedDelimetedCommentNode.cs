using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodes
{
    public class CgUnfinishedDelimitedCommentNode : CgTokenNodeBase, ICommentNode
    {
        private readonly string myText;

        public CgUnfinishedDelimitedCommentNode(string text)
        {
            myText = text;
        }

        public override NodeType NodeType => CgTokenNodeTypes.UNFINISHED_DELIMITED_COMMENT;

        public override int GetTextLength() => myText.Length;
        public override string GetText() => myText;
        public override bool IsFiltered() => true;
        
        public TreeTextRange GetCommentRange()
        {
            // remove slash and asterisk from the start
            var start = GetTreeStartOffset();
            return new TreeTextRange(start + 2, start + GetTextLength());
        }
        
        public string CommentText => myText.Substring(2);
    }
}