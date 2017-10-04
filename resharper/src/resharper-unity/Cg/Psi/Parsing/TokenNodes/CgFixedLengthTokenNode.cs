using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodes
{
    internal class CgFixedLengthTokenNode : CgTokenNodeBase
    {
        private readonly CgFixedLengthTokenNodeType myTokenNodeType;

        public CgFixedLengthTokenNode(CgFixedLengthTokenNodeType tokenNodeType)
        {
            myTokenNodeType = tokenNodeType;
        }

        public override int GetTextLength()
        {
            return myTokenNodeType.TokenRepresentation.Length;
        }

        public override string GetText()
        {
            return myTokenNodeType.TokenRepresentation;
        }

        public override NodeType NodeType => myTokenNodeType;
    }
}