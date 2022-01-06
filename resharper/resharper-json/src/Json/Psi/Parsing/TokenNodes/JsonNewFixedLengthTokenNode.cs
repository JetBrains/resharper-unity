using JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodes
{
    internal class JsonNewFixedLengthTokenNode : JsonNewTokenNodeBase
    {
        private readonly JsonNewFixedLengthTokenNodeType myTokenNodeType;

        public JsonNewFixedLengthTokenNode(JsonNewFixedLengthTokenNodeType tokenNodeType)
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