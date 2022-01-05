using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Parsing.TokenNodes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Parsing.TokenNodeTypes
{
    internal class JsonNewGenericTokenNodeType : JsonNewTokenNodeTypeBase
    {
        public JsonNewGenericTokenNodeType(string s, int index, string representation)
            : base(s, index)
        {
            TokenRepresentation = representation;
        }

        public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
        {
            return new JsonNewGenericTokenNode(this, buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
        }

        public override string TokenRepresentation { get; }
    }
}