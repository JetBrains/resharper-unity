using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Parsing.TokenNodes
{
    internal class JsonNewIdentifierTokenNode : JsonNewTokenNodeBase
    {
        private readonly string myText;

        public JsonNewIdentifierTokenNode(string text)
        {
            myText = text;
        }

        public override int GetTextLength() => myText.Length;
        public override string GetText() => myText;
        public override NodeType NodeType => JsonNewTokenNodeTypes.IDENTIFIER;

        public string Name => GetText();
    }
}