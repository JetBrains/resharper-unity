using JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodes
{
    public class JsonNewDelimitedCommentNode : JsonNewTokenNodeBase, ICommentNode
    {
        private readonly string myText;

        public JsonNewDelimitedCommentNode(string text)
        {
            myText = text;
        }

        public override NodeType NodeType => JsonNewTokenNodeTypes.DELIMITED_COMMENT;

        public override int GetTextLength() => myText.Length;
        public override string GetText() => myText;
        public override bool IsFiltered() => true;

        public TreeTextRange GetCommentRange()
        {
            // remove slash and asterisk from both ends
            var treeStartOffset = GetTreeStartOffset();
            return new TreeTextRange(treeStartOffset + 2, treeStartOffset + GetTextLength() - 2);
        }

        public string CommentText => myText.Substring(2, GetTextLength() - 2);
    }
}