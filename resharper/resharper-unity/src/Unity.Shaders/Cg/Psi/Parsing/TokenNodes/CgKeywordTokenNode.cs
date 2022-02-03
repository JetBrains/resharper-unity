using JetBrains.ReSharper.Plugins.Unity.Shaders.Cg.Psi.Parsing.TokenNodeTypes;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.Cg.Psi.Parsing.TokenNodes
{
    internal class CgKeywordTokenNode : CgFixedLengthTokenNode
    {
        public CgKeywordTokenNode(CgFixedLengthTokenNodeType tokenNodeType)
            : base(tokenNodeType)
        {
        }
    }
}