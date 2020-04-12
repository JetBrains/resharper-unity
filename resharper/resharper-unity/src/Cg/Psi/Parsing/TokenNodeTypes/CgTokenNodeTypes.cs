using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodeTypes
{
    public partial class CgTokenNodeTypes
    {
        public static readonly TokenNodeType BAD_CHARACTER = new CgGenericTokenNodeType("BAD_CHARACTER", LAST_GENERATED_TOKEN_TYPE_INDEX + 1, "�");
        
        public static readonly TokenNodeType WHITESPACE = new CgWhitespaceTokenNodeType(LAST_GENERATED_TOKEN_TYPE_INDEX + 2);
        
        public static readonly TokenNodeType NEW_LINE = new CgNewLineTokenNodeType(LAST_GENERATED_TOKEN_TYPE_INDEX + 3);
        
        public const int IDENTIFIER_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 4;
        public static readonly TokenNodeType IDENTIFIER = new CgIdentifierTokenNodeType(IDENTIFIER_NODE_TYPE_INDEX);
        
        public static readonly TokenNodeType SINGLE_LINE_COMMENT = new CgSingleLineCommentTokenNodeType(LAST_GENERATED_TOKEN_TYPE_INDEX + 5);
        
        public static readonly TokenNodeType DELIMITED_COMMENT = new CgDelimetedCommentNodeType(LAST_GENERATED_TOKEN_TYPE_INDEX + 6);
        
        public static readonly TokenNodeType UNFINISHED_DELIMITED_COMMENT = new CgUnfinishedDelimetedCommentNodeType(LAST_GENERATED_TOKEN_TYPE_INDEX + 7);
        
        public static readonly TokenNodeType EOF = new CgGenericTokenNodeType("EOF", LAST_GENERATED_TOKEN_TYPE_INDEX + 8, "EOF");
        
        public static readonly TokenNodeType NUMERIC_LITERAL = new CgNumericLiteralTokenNodeType(LAST_GENERATED_TOKEN_TYPE_INDEX + 9);

        public const int DIRECTIVE_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 10;
        public static readonly TokenNodeType DIRECTIVE = new CgPreprocessorDirectiveTokenNodeType(DIRECTIVE_NODE_TYPE_INDEX);

        public const int DIRECTIVE_CONTENT_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 11;
        public static readonly TokenNodeType DIRECTIVE_CONTENT = new CgFilteredGenericTokenNodeType("DIRECTIVE_CONTENT", DIRECTIVE_CONTENT_NODE_TYPE_INDEX, "directive content");
        
        public const int ASM_CONTENT_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 12;
        public static readonly TokenNodeType ASM_CONTENT = new CgGenericTokenNodeType("ASM_CONTENT", ASM_CONTENT_NODE_TYPE_INDEX, "asm content");

        public static readonly TokenNodeType SCALAR_TYPE = new CgBuiltInTypeTokenNodeType("SCALAR_TYPE", LAST_GENERATED_TOKEN_TYPE_INDEX + 13);
        
        public static readonly TokenNodeType VECTOR_TYPE = new CgBuiltInTypeTokenNodeType("VECTOR_TYPE", LAST_GENERATED_TOKEN_TYPE_INDEX + 14);
        
        public static readonly TokenNodeType MATRIX_TYPE = new CgBuiltInTypeTokenNodeType("MATRIX_TYPE", LAST_GENERATED_TOKEN_TYPE_INDEX + 15);
    }
}