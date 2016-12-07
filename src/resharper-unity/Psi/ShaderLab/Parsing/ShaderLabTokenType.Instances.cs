using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Parsing
{
    public partial class ShaderLabTokenType
    {
        public const int IDENTIFIER_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 5;
        public const int STRING_LITERAL_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 6;

        public static readonly TokenNodeType NEW_LINE = new NewLineNodeType(LAST_GENERATED_TOKEN_TYPE_INDEX + 1);
        public static readonly TokenNodeType WHITESPACE = new WhitespaceNodeType(LAST_GENERATED_TOKEN_TYPE_INDEX + 2);
        public static readonly TokenNodeType END_OF_LINE_COMMENT = new EndOfLineCommentNodeType(LAST_GENERATED_TOKEN_TYPE_INDEX + 3);
        public static readonly TokenNodeType MULTI_LINE_COMMENT = new MultiLineCommentNodeType(LAST_GENERATED_TOKEN_TYPE_INDEX + 4);
        public static readonly TokenNodeType IDENTIFIER = new IdentifierNodeType(IDENTIFIER_NODE_TYPE_INDEX);
        public static readonly TokenNodeType STRING_LITERAL = new GenericTokenNodeType("STRING_LITERAL", STRING_LITERAL_NODE_TYPE_INDEX, "\"XXX\"");
        public static readonly TokenNodeType INTEGER_LITERAL = new GenericTokenNodeType("INTEGER_LITERAL", LAST_GENERATED_TOKEN_TYPE_INDEX + 7, "000");
        public static readonly TokenNodeType FLOAT_LITERAL = new GenericTokenNodeType("FLOAT_LITERAL", LAST_GENERATED_TOKEN_TYPE_INDEX + 8, "0.0");
        public static readonly TokenNodeType CG_CONTENT = new GenericTokenNodeType("CG_CONTENT", LAST_GENERATED_TOKEN_TYPE_INDEX + 9, "CG_CONTENT");
    }
}