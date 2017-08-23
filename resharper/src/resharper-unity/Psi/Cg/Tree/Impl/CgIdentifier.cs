using JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Parsing.TokenNodes;
using JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Tree.Impl
{
    internal class CgIdentifier : CgTokenNodeBase
    {
        private readonly string myText;

        public CgIdentifier(string text)
        {
            myText = text;
        }

        public override int GetTextLength() => myText.Length;
        public override string GetText() => myText;
        public override NodeType NodeType => CgTokenNodeTypes.IDENTIFIER;

        public string Name => GetText();
    }
}