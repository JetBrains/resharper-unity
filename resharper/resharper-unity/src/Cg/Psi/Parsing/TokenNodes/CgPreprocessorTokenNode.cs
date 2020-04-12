using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodes
{
    internal class CgPreprocessorTokenNode : CgGenericTokenNode
    {
        public CgPreprocessorTokenNode(TokenNodeType tokenNodeType, string text)
            : base(tokenNodeType, text)
        {
        }
    }
}