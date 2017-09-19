using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodeTypes
{
    internal class CgPreprocessorTokenNodeType : CgTokenNodeTypeBase
    {
        public CgPreprocessorTokenNodeType(string s, int index)
            : base(s, index)
        {
        }

        public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
        {
            throw new System.NotImplementedException();
        }

        public override string TokenRepresentation { get; }
    }
}