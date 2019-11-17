using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Parsing.TokenNodes;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Parsing.TokenNodeTypes;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Parsing.TokenNodes
{
    internal class JsonNewKeywordTokenNode : JsonNewFixedLengthTokenNode
    {
        public JsonNewKeywordTokenNode(JsonNewFixedLengthTokenNodeType tokenNodeType)
            : base(tokenNodeType)
        {
        }
    }
}