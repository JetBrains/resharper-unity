using JetBrains.ReSharper.Plugins.Unity.Shaders.Cg.Psi.Parsing.TokenNodes;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Resources;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.Cg.Psi.Parsing.TokenNodeTypes
{
    internal class CgBuiltInTypeTokenNodeType : CgTokenNodeTypeBase
    {
        public CgBuiltInTypeTokenNodeType(string s, int index)
            : base(s, index)
        {
        }

        public override bool IsKeyword => true;

        public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
        {
            return new CgBuiltInTypeTokenNode(this, buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
        }

        public override LeafElementBase Create(string token)
        {
            return new CgBuiltInTypeTokenNode(this, token);
        }

        public override string TokenRepresentation => Strings.CgBuiltInTypeTokenNodeType_TokenRepresentation_built_in_type;
    }
}