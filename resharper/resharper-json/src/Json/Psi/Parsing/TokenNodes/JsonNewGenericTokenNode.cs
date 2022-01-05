using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodes
{
    internal class JsonNewGenericTokenNode : JsonNewTokenNodeBase
    {
        private readonly TokenNodeType myTokenNodeType;
        private readonly string myText;

        public JsonNewGenericTokenNode(TokenNodeType tokenNodeType, string text)
        {
            myTokenNodeType = tokenNodeType;
            myText = text;
        }

        public override NodeType NodeType => myTokenNodeType;
        public override int GetTextLength() => myText.Length;
        public override string GetText() => myText;
    }
}