using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Parsing.TokenNodeTypes
{
    public partial class CgTokenNodeTypes
    {
        public static readonly TokenNodeType BAD_CHARACTER = new CgGenericTokenNodeType("BAD_CHARACTER", LAST_GENERATED_TOKEN_TYPE_INDEX + 1, "�");
        
        public static readonly TokenNodeType WHITESPACE = new CgWhitespaceTokenNodeType(LAST_GENERATED_TOKEN_TYPE_INDEX + 2);
        
        public static readonly TokenNodeType NEW_LINE = new CgNewLineTokenNodeType(LAST_GENERATED_TOKEN_TYPE_INDEX + 3);
        
        public static readonly TokenNodeType IDENTIFIER = new CgIdentifierTokenNodeType(LAST_GENERATED_TOKEN_TYPE_INDEX + 4);
        
        public static readonly TokenNodeType SINGLE_LINE_COMMENT = new CgSingleLineCommentTokenNodeType(LAST_GENERATED_TOKEN_TYPE_INDEX + 5);
    }
}