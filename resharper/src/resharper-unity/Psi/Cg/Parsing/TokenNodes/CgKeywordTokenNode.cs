using JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Parsing.TokenNodeTypes;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Parsing.TokenNodes
{
    internal class CgKeywordTokenNode : CgFixedLengthTokenNode
    {
        public CgKeywordTokenNode(CgFixedLengthTokenNodeType tokenNodeType)
            : base(tokenNodeType)
        {
        }
    }
}