using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.Cg.Psi.Parsing.TokenNodes
{
    internal class CgPreprocessorTokenNode : CgGenericTokenNode
    {
        public CgPreprocessorTokenNode(TokenNodeType tokenNodeType, string text)
            : base(tokenNodeType, text)
        {
        }
    }
}