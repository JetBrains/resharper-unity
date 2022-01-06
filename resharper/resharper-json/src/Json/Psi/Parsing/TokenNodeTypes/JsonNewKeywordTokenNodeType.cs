using JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodeTypes
{
    internal class JsonNewKeywordTokenNodeType : JsonNewFixedLengthTokenNodeType
    {
        public JsonNewKeywordTokenNodeType(string s, int index, string representation)
            : base(s, index, representation)
        {
        }

        public override bool IsKeyword => true;

        public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
        {
            return new JsonNewKeywordTokenNode(this);
        }
    }
}