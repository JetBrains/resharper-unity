using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Parsing.TokenNodes
{
    internal class JsonNewNewLineTokenNode : JsonNewTokenNodeBase, IWhitespaceNode
    {
        private readonly string myText;

        public JsonNewNewLineTokenNode(string text)
        {
            myText = text;
        }

        public override int GetTextLength() => myText.Length;

        public override string GetText() => myText;

        public override NodeType NodeType => JsonNewTokenNodeTypes.NEW_LINE;

        public bool IsNewLine => true;
    }
}