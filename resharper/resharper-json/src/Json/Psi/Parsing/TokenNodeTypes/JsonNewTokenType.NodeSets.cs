using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodeTypes
{
    public partial class JsonNewTokenNodeTypes
    {
        public static readonly NodeTypeSet LITERALS;
        public static readonly NodeTypeSet COMMENTS_AND_WHITESPACES;

        static JsonNewTokenNodeTypes()
        {
            LITERALS = new NodeTypeSet(
                TRUE_KEYWORD,
                FALSE_KEYWORD,
                NULL_KEYWORD,
                IDENTIFIER,
                DOUBLE_QUOTED_STRING,
                NUMERIC_LITERAL
            );

            COMMENTS_AND_WHITESPACES = new NodeTypeSet(
                WHITE_SPACE,
                DELIMITED_COMMENT,
                SINGLE_LINE_COMMENT,
                NEW_LINE
            );
        }
    }
}