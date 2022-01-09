using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodeTypes;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodes
{
    internal class CgKeywordTokenNode : CgFixedLengthTokenNode
    {
        public CgKeywordTokenNode(CgFixedLengthTokenNodeType tokenNodeType)
            : base(tokenNodeType)
        {
        }
    }
}