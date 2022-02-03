using JetBrains.ReSharper.Plugins.Unity.Shaders.Cg.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.Cg.Psi.Parsing.TokenNodes
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