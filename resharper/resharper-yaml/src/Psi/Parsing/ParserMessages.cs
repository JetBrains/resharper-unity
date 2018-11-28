using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing
{
  public static class ParserMessages
  {
    public const string IDS_BLOCK_MAPPING_ENTRY = "mapping pair";
    public const string IDS_CHOMPING_INDICATOR = "chomping indicator";
    public const string IDS_DIRECTIVE = "directive";
    public const string IDS_DOUBLE_QUOTED_SCALAR_NODE = "double quoted scalar";
    public const string IDS_PLAIN_SCALAR_NODE = "plain scalar";
    public const string IDS_SEQUENCE_ENTRY = "sequence item";
    public const string IDS_SINGLE_QUOTED_SCALAR_NODE = "single quoted scalar";
    public const string IDS_TAG_HANDLE = "tag handle";
    public const string IDS_TAG_PROPERTY = "tag property";

    public static string GetString(string id) => id;

    public static string GetUnexpectedTokenMessage() => "Unexpected token";

    public static string GetExpectedMessage(string expectedSymbol)
    {
      return string.Format(GetString("{0} expected"), expectedSymbol).Capitalize();
    }

    public static string GetExpectedMessage(string firstExpectedSymbol, string secondExpectedSymbol)
    {
      return string.Format(GetString("{0} or {1} expected"), firstExpectedSymbol, secondExpectedSymbol).Capitalize();
    }
  }
}