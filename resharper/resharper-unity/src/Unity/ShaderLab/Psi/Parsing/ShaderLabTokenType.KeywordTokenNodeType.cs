using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing
{
    public partial class ShaderLabTokenType
    {
        private class KeywordTokenNodeType : FixedTokenNodeType
        {
            public KeywordTokenNodeType(string s, int index, string representation)
                : base(s, index, representation)
            {
            }

            public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
            {
                return new KeywordTokenElement(this, buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
            }

            public override bool IsKeyword => true;
        }

        public class KeywordTokenElement : ShaderLabTokenBase
        {
            private readonly TokenNodeType myTokenNodeType;
            private readonly string myText;

            // Keywords need to take the actual text because they are case insensitive -
            // we can't normalise them
            public KeywordTokenElement(TokenNodeType tokenNodeType, string text)
            {
                myTokenNodeType = tokenNodeType;
                myText = text;
            }

            public override NodeType NodeType => myTokenNodeType;
            public override int GetTextLength() => myText.Length;
            public override string GetText() => myText;
        }
    }
}