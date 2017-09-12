using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree.Impl
{
    internal abstract class WhitespaceBase : ShaderLabTokenBase, IWhitespaceNode
    {
        [NotNull] private readonly string myText;

        protected WhitespaceBase([NotNull] string text)
        {
            myText = text;
        }

        public override int GetTextLength() => myText.Length;
        public override string GetText() => myText;
        public abstract bool IsNewLine { get; }
        public override bool IsFiltered() => true;

        public override string ToString()
        {
            return base.ToString() + " spaces: " + "\"" + GetText() + "\"";
        }
    }

    internal class Whitespace : WhitespaceBase
    {
        public Whitespace([NotNull] string text)
            : base(text)
        {
        }

        public override NodeType NodeType => ShaderLabTokenType.WHITESPACE;
        public override bool IsNewLine => false;
    }

    internal class NewLine : WhitespaceBase
    {
        public NewLine([NotNull] string text)
            : base(text)
        {
        }

        public override NodeType NodeType => ShaderLabTokenType.NEW_LINE;
        public override bool IsNewLine => true;
    }

}