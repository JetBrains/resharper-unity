using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Parsing.TokenNodes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Parsing.TokenNodeTypes
{
    internal class JsonNewNewLineTokenNodeType : JsonNewTokenNodeTypeBase
    {
        public JsonNewNewLineTokenNodeType(int index)
            : base("NEW_LINE", index)
        {
        }

        public override LeafElementBase Create(string token)
        {
            return new JsonNewNewLineTokenNode(token);
        }

        public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
        {
            return new JsonNewNewLineTokenNode(buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
        }

        public override bool IsWhitespace => true;
        public override string TokenRepresentation => @"\r\n";
    }
}