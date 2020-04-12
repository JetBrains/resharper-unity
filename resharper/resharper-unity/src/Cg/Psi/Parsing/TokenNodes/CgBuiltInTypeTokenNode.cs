using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodes
{
    internal class CgBuiltInTypeTokenNode : CgTokenNodeBase
    {
        private readonly string myText;

        public CgBuiltInTypeTokenNode(CgBuiltInTypeTokenNodeType nodeType, string text)
        {
            NodeType = nodeType;
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

        public override NodeType NodeType { get; }
    }
}