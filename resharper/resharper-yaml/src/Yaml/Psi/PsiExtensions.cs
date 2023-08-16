#nullable enable
using System.Text;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.Text;
using JetBrains.Util;
using JetBrains.Util.Text;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi
{
  public static class PsiExtensions
  {
    public static string? GetScalarText(this INode? node) => node switch
    {
      IPlainScalarNode plainScalarNode => plainScalarNode.Text.GetText(),
      ISingleQuotedScalarNode singleQuotedScalarNode => GetUnquotedText(singleQuotedScalarNode.Text.GetText(), '\'', '\''),
      IDoubleQuotedScalarNode doubleQuotedScalarNode => GetUnquotedText(doubleQuotedScalarNode.Text.GetText(), '"', '\\'),
      _ => null
    };

    private static unsafe string? GetUnquotedText(string text, char quoteCharacter, char escapeCharacter)
    {
      if (text.Length < 2 || text[0] != quoteCharacter || text[^1] != quoteCharacter)
        return null;
      if (text.Length == 2)
        return string.Empty;
      var result = new StringBuilder(text.Length - 2);
      fixed (char* chars = text) 
        ProcessEscapedString(chars + 1, chars + text.Length - 1, quoteCharacter, escapeCharacter, result);
      return result.ToString();
    }

    private static unsafe void ProcessEscapedString(char* ptr, char* endPtr, char quoteCharacter, char escapeCharacter, StringBuilder output)
    {
      var inEscapeSequence = false;
      while (ptr < endPtr)
      {
        var ch = *ptr++;
        if (inEscapeSequence)
        {
          if (ch != escapeCharacter && ch != quoteCharacter)
          {
            output.Append(escapeCharacter);
            output.Append(ch);
          }
          else
            output.Append(ch);

          inEscapeSequence = false;
        }
        else
        {
          if (ch != escapeCharacter)
            output.Append(ch);
          else
            inEscapeSequence = true;
        }
      }
    }

    public static IBuffer? GetPlainScalarTextBuffer(this INode? node) => (node as IPlainScalarNode)?.Text.GetTextAsBuffer();

    private static bool MatchesPlainScalarText(this INode? node, string value)
    {
      if (node is IPlainScalarNode scalar)
      {
        var bufferRange = new BufferRange(scalar.Text.GetTextAsBuffer(), new TextRange(0, scalar.Text.GetTextLength()));
        return bufferRange.StringEquals(value);
      }

      return false;
    }

    public static IBlockMappingEntry? GetMapEntry(this IBlockMappingNode? mapNode, string simpleKey)
    {
      if (mapNode == null)
        return null;

      foreach (var mappingEntry in mapNode.EntriesEnumerable)
      {
        if (mappingEntry.Key.MatchesPlainScalarText(simpleKey))
          return mappingEntry;
      }

      return null;
    }

    public static IFlowMapEntry? GetMapEntry(this IFlowMappingNode? mapNode, string simpleKey)
    {
      if (mapNode == null)
        return null;

      foreach (var sequenceEntry in mapNode.EntriesEnumerable)
      {
        if (sequenceEntry.Key.MatchesPlainScalarText(simpleKey))
          return sequenceEntry;
      }

      return null;
    }

    public static T? GetMapEntryValue<T>(this IBlockMappingNode? mapNode, string simpleKey)
      where T : class, INode
    {
      return mapNode.GetMapEntry(simpleKey)?.Content?.Value as T;
    }

    public static string? GetMapEntryScalarText(this IBlockMappingNode? mapNode, string simpleKey) => mapNode.GetMapEntry(simpleKey)?.Content?.Value.GetScalarText();

    public static string? GetMapEntryScalarText(this IFlowMappingNode? mapNode, string simpleKey) => mapNode.GetMapEntry(simpleKey)?.Value.GetScalarText();
  }
}