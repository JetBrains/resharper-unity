using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Parsing.TokenNodes
{
    internal class CgGenericTokenNode : CgTokenNodeBase
    {
        private readonly TokenNodeType myTokenNodeType;
        private readonly string myText;

        public CgGenericTokenNode(TokenNodeType tokenNodeType, string text)
        {
            myTokenNodeType = tokenNodeType;
            myText = text;
        }

        public override NodeType NodeType => myTokenNodeType;
        public override int GetTextLength() => myText.Length;
        public override string GetText() => myText;
    }
}