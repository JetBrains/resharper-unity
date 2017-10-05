using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree.Impl
{
    internal abstract class CommentBase : ShaderLabTokenBase, ICommentNode
    {
        [NotNull] private readonly string myText;

        protected CommentBase([NotNull] string text)
        {
            myText = text;
        }

        public override int GetTextLength() => myText.Length;
        public override string GetText() => myText;
        public override bool IsFiltered() => true;

        public abstract string CommentText { get; }
        public abstract TreeTextRange GetCommentRange();

        public override string ToString()
        {
            return base.ToString() + " spaces: " + "\"" + GetText() + "\"";
        }
    }

    internal class EndOfLineComment : CommentBase
    {
        public EndOfLineComment([NotNull] string text)
            : base(text)
        {
        }

        public override NodeType NodeType => ShaderLabTokenType.END_OF_LINE_COMMENT;

        public override TreeTextRange GetCommentRange()
        {
            var treeStartOffset = GetTreeStartOffset();
            return new TreeTextRange(treeStartOffset + 2, treeStartOffset + GetTextLength());
        }

        public override string CommentText => GetText().Substring(2);
    }

    internal class MultiLineComment : CommentBase
    {
        public MultiLineComment([NotNull] string text)
            : base(text)
        {
        }

        public override NodeType NodeType => ShaderLabTokenType.MULTI_LINE_COMMENT;

        public override TreeTextRange GetCommentRange()
        {
                var treeStartOffset = GetTreeStartOffset();
                var text = GetText();
                var length = text.Length - (text.EndsWith("*/") ? 4 : 2);
                return length <= 0 ? TreeTextRange.InvalidRange : new TreeTextRange(treeStartOffset + 2, treeStartOffset + 2 + length);
        }

        public override string CommentText
        {
            get
            {
                var text = GetText();
                var length = text.Length - (text.EndsWith("*/") ? 4 : 2);
                return length > 0 ? text.Substring(2, length) : string.Empty;
            }
        }
    }
}