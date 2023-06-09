using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Parsing
{
    public class UnityYamlTokenType
    {
        private class UnityDocumentTokenType : YamlTokenType.YamlTokenNodeType
        {
            public UnityDocumentTokenType(string s, int index)
                : base(s, index)
            {
            }

            public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
            {
                return new ClosedChameleonElement(YamlTokenType.CHAMELEON, buffer, startOffset, endOffset.Offset - startOffset.Offset);
            }

            public override string TokenRepresentation => ToString();
        }

        private class UnityUselessDocumentTokenType : YamlTokenType.YamlTokenNodeType
        {
            public UnityUselessDocumentTokenType(string s, int index)
                : base(s, index)
            {
            }

            public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
            {
                return new YamlTokenType.GenericTokenElement(this, buffer, startOffset, endOffset);
            }

            public override string TokenRepresentation => ToString();
        }

        public static readonly TokenNodeType DOCUMENT = new UnityDocumentTokenType("DOCUMENT", 2001);

        public static readonly TokenNodeType USELESS_DOCUMENT =
            new UnityUselessDocumentTokenType("USELESS_DOCUMENT", 2002);
    }
}