using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Parsing.TokenNodeTypes
{
    public partial class JsonNewTokenNodeTypes
    {
        public static readonly TokenNodeType BAD_CHARACTER = new JsonNewGenericTokenNodeType("BAD_CHARACTER", LAST_GENERATED_TOKEN_TYPE_INDEX + 1, "�");
        
        public static readonly TokenNodeType WHITE_SPACE = new JsonNewWhitespaceTokenNodeType(LAST_GENERATED_TOKEN_TYPE_INDEX + 2);
        
        public static readonly TokenNodeType NEW_LINE = new JsonNewNewLineTokenNodeType(LAST_GENERATED_TOKEN_TYPE_INDEX + 3);
        
        public const int IDENTIFIER_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 4;
        public static readonly TokenNodeType IDENTIFIER = new JsonNewIdentifierTokenNodeType(IDENTIFIER_NODE_TYPE_INDEX);
        
        public static readonly TokenNodeType SINGLE_LINE_COMMENT = new JsonNewLineCommentTokenNodeType(IDENTIFIER_NODE_TYPE_INDEX + 1);
        
        public static readonly TokenNodeType DELIMITED_COMMENT = new JsonNewDelimetedCommentNodeType(IDENTIFIER_NODE_TYPE_INDEX + 2);
        
        public static readonly TokenNodeType EOF = new JsonNewGenericTokenNodeType("EOF", LAST_GENERATED_TOKEN_TYPE_INDEX + 3, "EOF");
        
        public const int NUMERIC_LITERAL_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 4;
        public static readonly TokenNodeType NUMERIC_LITERAL = new JsonNewNumericLiteralTokenNodeType(NUMERIC_LITERAL_NODE_TYPE_INDEX);
        
        public const int SINGLE_QUOTED_STRING_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 5;
        public static readonly TokenNodeType SINGLE_QUOTED_STRING = new JsonNewSingleQuotedStringTokenNodeType(SINGLE_QUOTED_STRING_NODE_TYPE_INDEX);
        
        public const int DOUBLE_QUOTED_STRING_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 6;
        public static readonly TokenNodeType DOUBLE_QUOTED_STRING = new JsonNewDoubleQuotedStringTokenNodeType(DOUBLE_QUOTED_STRING_NODE_TYPE_INDEX);
 
    }
}