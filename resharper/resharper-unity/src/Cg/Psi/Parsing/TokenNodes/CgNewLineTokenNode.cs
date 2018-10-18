using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodes
{
    internal class CgNewLineTokenNode : CgWhitespaceTokenNodeBase
    {
        public CgNewLineTokenNode(string text)
            : base(text)
        {
        }

        public override NodeType NodeType => CgTokenNodeTypes.NEW_LINE;

        public override bool IsNewLine => true;
    }
}