using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodes
{
    internal class CgIdentifierTokenNode : CgTokenNodeBase
    {
        private readonly string myText;

        public CgIdentifierTokenNode(string text)
        {
            myText = text;
        }

        public override int GetTextLength() => myText.Length;
        public override string GetText() => myText;
        public override NodeType NodeType => CgTokenNodeTypes.IDENTIFIER;

        public string Name => GetText();
    }
}