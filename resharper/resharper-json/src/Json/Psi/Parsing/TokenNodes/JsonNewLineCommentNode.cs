using JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodes
{
    public class JsonNewLineCommentNode : JsonNewTokenNodeBase, ICommentNode
    {
        private readonly string myText;

        public JsonNewLineCommentNode(string text)
        {
            myText = text;
        }

        public override NodeType NodeType => JsonNewTokenNodeTypes.SINGLE_LINE_COMMENT;

        public override int GetTextLength() => myText.Length;
        public override string GetText() => myText;
        public override bool IsFiltered() => true;

        public TreeTextRange GetCommentRange()
        {
            // remove two slashes
            var start = GetTreeStartOffset();
            return new TreeTextRange(start + 2, start + GetTextLength());
        }

        public string CommentText => myText.Substring(2);
    }
}