using JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodeTypes
{
    internal class JsonNewFixedLengthTokenNodeType : JsonNewTokenNodeTypeBase
    {
        public JsonNewFixedLengthTokenNodeType(string s, int index, string representation)
            : base(s, index)
        {
            TokenRepresentation = representation;
        }

        public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
        {
            return new JsonNewFixedLengthTokenNode(this);
        }

        public override string TokenRepresentation { get; }
    }
}