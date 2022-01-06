using JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodeTypes;

namespace JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodes
{
    internal class JsonNewKeywordTokenNode : JsonNewFixedLengthTokenNode
    {
        public JsonNewKeywordTokenNode(JsonNewFixedLengthTokenNodeType tokenNodeType)
            : base(tokenNodeType)
        {
        }
    }
}