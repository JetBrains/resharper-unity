using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Parsing
{
    public partial class ShaderLabTokenType
    {
        public const int IDENTIFIER_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 1;
        public const int STRING_LITERAL_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 2;
        public const int NUMERIC_LITERAL_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 3;
        public const int CG_CONTENT_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 4;

        public static readonly TokenNodeType NEW_LINE = new NewLineNodeType(LAST_GENERATED_TOKEN_TYPE_INDEX + 10);
        public static readonly TokenNodeType WHITESPACE = new WhitespaceNodeType(LAST_GENERATED_TOKEN_TYPE_INDEX + 11);
        public static readonly TokenNodeType END_OF_LINE_COMMENT = new EndOfLineCommentNodeType(LAST_GENERATED_TOKEN_TYPE_INDEX + 12);
        public static readonly TokenNodeType MULTI_LINE_COMMENT = new MultiLineCommentNodeType(LAST_GENERATED_TOKEN_TYPE_INDEX + 13);

        public static readonly TokenNodeType BAD_CHARACTER = new GenericTokenNodeType("BAD_CHARACTER", LAST_GENERATED_TOKEN_TYPE_INDEX + 14, "�");

        public static readonly TokenNodeType PP_MESSAGE = new FilteredGenericTokenNodeType("PP_MESSAGE", LAST_GENERATED_TOKEN_TYPE_INDEX + 15, "\"message\"");
        public static readonly TokenNodeType PP_DIGITS = new FilteredGenericTokenNodeType("PP_DIGITS", LAST_GENERATED_TOKEN_TYPE_INDEX + 16, "1234");
        public static readonly TokenNodeType PP_SWALLOWED = new FilteredGenericTokenNodeType("PP_SWALLOWED", LAST_GENERATED_TOKEN_TYPE_INDEX + 17, "�");

        public static readonly TokenNodeType EOF = new GenericTokenNodeType("EOF", LAST_GENERATED_TOKEN_TYPE_INDEX + 18, "EOF");

        public static readonly TokenNodeType IDENTIFIER = new IdentifierNodeType(IDENTIFIER_NODE_TYPE_INDEX);
        public static readonly TokenNodeType STRING_LITERAL = new StringLiteralNodeType(STRING_LITERAL_NODE_TYPE_INDEX);
        public static readonly TokenNodeType NUMERIC_LITERAL = new NumericLiteralNodeType(NUMERIC_LITERAL_NODE_TYPE_INDEX);
        public static readonly TokenNodeType CG_CONTENT = new GenericTokenNodeType("CG_CONTENT", CG_CONTENT_NODE_TYPE_INDEX, "CG_CONTENT");
    }
}
