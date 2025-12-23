using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing
{
  public partial class YamlTokenType
  {
    public const int C_DOUBLE_QUOTED_MULTI_LINE_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 1;
    public const int C_DOUBLE_QUOTED_SINGLE_LINE_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 2;
    public const int C_SINGLE_QUOTED_SINGLE_LINE_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 3;
    public const int C_SINGLE_QUOTED_MULTI_LINE_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 4;
    public const int INDENT_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 5;
    public const int NS_ANCHOR_NAME_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 6;
    public const int NS_DEC_DIGIT_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 7;
    public const int NS_PLAIN_MULTI_LINE_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 8;
    public const int NS_PLAIN_ONE_LINE_IN_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 9;
    public const int NS_PLAIN_ONE_LINE_OUT_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 10;
    public const int NS_TAG_CHARS_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 11;
    public const int NS_URI_CHARS_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 12;
    public const int NS_WORD_CHARS_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 13;
    public const int SCALAR_TEXT_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 14;
    public const int CHAMELEON_BLOCK_MAPPING_ENTRY_CONTENT_WITH_INDENT_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 15;




    public static readonly TokenNodeType BAD_CHARACTER = new GenericTokenNodeType("BAD_CHARACTER", LAST_GENERATED_TOKEN_TYPE_INDEX + 20, "�");
    public static readonly TokenNodeType NON_PRINTABLE = new GenericTokenNodeType("NON_PRINTABLE", LAST_GENERATED_TOKEN_TYPE_INDEX + 21, "�");

    public static readonly TokenNodeType EOF = new GenericTokenNodeType("EOF", LAST_GENERATED_TOKEN_TYPE_INDEX + 22, "EOF");

    public static readonly TokenNodeType CHAMELEON = new GenericTokenNodeType("CHAMELEON", LAST_GENERATED_TOKEN_TYPE_INDEX + 23, "CHAMELEON");

    public static readonly TokenNodeType NEW_LINE = new NewLineNodeType(LAST_GENERATED_TOKEN_TYPE_INDEX + 24);
    public static readonly TokenNodeType WHITESPACE = new WhitespaceNodeType(LAST_GENERATED_TOKEN_TYPE_INDEX + 25);
    public static readonly TokenNodeType INDENT = new GenericTokenNodeType("INDENT", INDENT_NODE_TYPE_INDEX, "INDENT");

    public static readonly TokenNodeType COMMENT = new CommentTokenNodeType(LAST_GENERATED_TOKEN_TYPE_INDEX + 26);

    // TODO: Naming. The YAML spec has an interesting hungarian notation style...
    // Should NS_URI_CHARS, NS_TAG_CHARS, NS_PLAIN and NS_ANCHOR_NAME just become some kind of "VALUE"?
    // I don't think the parser would really care what type of textual value it is - as long as the value is there
    public static readonly TokenNodeType NS_CHARS = new GenericTokenNodeType("NS_CHARS", LAST_GENERATED_TOKEN_TYPE_INDEX + 30, "NS_CHARS");
    public static readonly TokenNodeType NS_WORD_CHARS = new GenericTokenNodeType("NS_WORD_CHARS", NS_WORD_CHARS_NODE_TYPE_INDEX, "NS_WORD_CHARS");
    public static readonly TokenNodeType NS_URI_CHARS = new GenericTokenNodeType("NS_URI_CHARS", NS_URI_CHARS_NODE_TYPE_INDEX, "NS_URI_CHARS");
    public static readonly TokenNodeType NS_TAG_CHARS = new GenericTokenNodeType("NS_TAG_CHARS", NS_TAG_CHARS_NODE_TYPE_INDEX, "NS_TAG_CHARS");
    // These node types have the same text to help with testing. Changing the text doesn't help much, but would require updating ALL gold files
    public static readonly TokenNodeType NS_PLAIN_ONE_LINE_IN = new GenericTokenNodeType("NS_PLAIN", NS_PLAIN_ONE_LINE_IN_NODE_TYPE_INDEX, "NS_PLAIN_IN");
    public static readonly TokenNodeType NS_PLAIN_ONE_LINE_OUT = new GenericTokenNodeType("NS_PLAIN", NS_PLAIN_ONE_LINE_OUT_NODE_TYPE_INDEX, "NS_PLAIN_OUT");
    public static readonly TokenNodeType NS_PLAIN_MULTI_LINE = new GenericTokenNodeType("NS_PLAIN", NS_PLAIN_MULTI_LINE_NODE_TYPE_INDEX, "NS_PLAIN_MULTI_LINE");
    public static readonly TokenNodeType NS_ANCHOR_NAME = new GenericTokenNodeType("NS_ANCHOR_NAME", NS_ANCHOR_NAME_NODE_TYPE_INDEX, "NS_ANCHOR_NAME");
    public static readonly TokenNodeType C_SINGLE_QUOTED_SINGLE_LINE = new GenericTokenNodeType("C_SINGLE_QUOTED_SINGLE_LINE", C_SINGLE_QUOTED_SINGLE_LINE_NODE_TYPE_INDEX, "C_SINGLE_QUOTED_SINGLE_LINE");
    public static readonly TokenNodeType C_SINGLE_QUOTED_MULTI_LINE = new GenericTokenNodeType("C_SINGLE_QUOTED_MULTILINE", C_SINGLE_QUOTED_MULTI_LINE_NODE_TYPE_INDEX, "C_SINGLE_QUOTED_MULTILINE");
    public static readonly TokenNodeType C_DOUBLE_QUOTED_SINGLE_LINE = new GenericTokenNodeType("C_DOUBLE_QUOTED_SINGLE_LINE", C_DOUBLE_QUOTED_SINGLE_LINE_NODE_TYPE_INDEX, "C_DOUBLE_QUOTED_SINGLE_LINE");
    public static readonly TokenNodeType C_DOUBLE_QUOTED_MULTI_LINE = new GenericTokenNodeType("C_DOUBLE_QUOTED_MULTILINE", C_DOUBLE_QUOTED_MULTI_LINE_NODE_TYPE_INDEX, "C_DOUBLE_QUOTED_MULTILINE");
    public static readonly TokenNodeType NS_DEC_DIGIT = new GenericTokenNodeType("NS_DEC_DIGIT", NS_DEC_DIGIT_NODE_TYPE_INDEX, "NS_DEC_DIGIT");
    public static readonly TokenNodeType SCALAR_TEXT = new GenericTokenNodeType("SCALAR_TEXT", SCALAR_TEXT_NODE_TYPE_INDEX, "SCALAR_TEXT");

    
    public static readonly TokenNodeType CHAMELEON_BLOCK_MAPPING_ENTRY_CONTENT_WITH_ANY_INDENT = GetChameleonTokenNodeTypeWithIndentInternal(-1);

    private static ChameleonTokenNodeType GetChameleonTokenNodeTypeWithIndentInternal(int indent)
    {
      return new ChameleonTokenNodeType("CHAMELEON_BLOCK_MAPPING_ENTRY_CONTENT_WITH_INDENT", indent, CHAMELEON_BLOCK_MAPPING_ENTRY_CONTENT_WITH_INDENT_INDEX, "CHAMELEON_BLOCK_MAPPING_ENTRY_CONTENT_WITH_INDENT");
    }

    // reuse the most popular indents
    private static readonly ChameleonTokenNodeType[] CHAMELEON_BLOCK_MAPPING_ENTRY_CONTENT_WITH_INDENT =
      new ChameleonTokenNodeType[]
      {
        GetChameleonTokenNodeTypeWithIndentInternal(0), GetChameleonTokenNodeTypeWithIndentInternal(1),
        GetChameleonTokenNodeTypeWithIndentInternal(2), GetChameleonTokenNodeTypeWithIndentInternal(3),
        GetChameleonTokenNodeTypeWithIndentInternal(4), GetChameleonTokenNodeTypeWithIndentInternal(5),
        GetChameleonTokenNodeTypeWithIndentInternal(6), GetChameleonTokenNodeTypeWithIndentInternal(7),
        GetChameleonTokenNodeTypeWithIndentInternal(8), GetChameleonTokenNodeTypeWithIndentInternal(9),
        GetChameleonTokenNodeTypeWithIndentInternal(10),
      };

    public static TokenNodeType GetChameleonMapEntryValueWithIndent(int indent)
    {
      if (indent <= 10)
        return CHAMELEON_BLOCK_MAPPING_ENTRY_CONTENT_WITH_INDENT[indent];

      return GetChameleonTokenNodeTypeWithIndentInternal(indent);
    }
  }
}
