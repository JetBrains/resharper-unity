using JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Parsing.TokenNodes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Parsing.TokenNodeTypes
{
    internal class CgKeywordTokenNodeType : CgFixedLengthTokenNodeType
    {
        public CgKeywordTokenNodeType(string s, int index, string representation)
            : base(s, index, representation)
        {
        }

        public override bool IsKeyword => true;

        public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
        {
            return new CgKeywordTokenNode(this);
        }
    }
}