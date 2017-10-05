using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodes
{
    internal abstract class CgWhitespaceTokenNodeBase : CgTokenNodeBase, IWhitespaceNode
    {
        private readonly string myText;

        protected CgWhitespaceTokenNodeBase(string text)
        {
            myText = text;
        }

        public override int GetTextLength()
        {
            return myText.Length;
        }

        public override string GetText()
        {
            return myText;
        }

        public abstract bool IsNewLine { get; }
        
        public override bool IsFiltered() => true;

        public override string ToString()
        {
            return base.ToString() + " spaces: " + "\"" + GetText() + "\"";
        }
    }
}