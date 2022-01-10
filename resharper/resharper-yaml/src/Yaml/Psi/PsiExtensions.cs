using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.Text;
using JetBrains.Util;
using JetBrains.Util.Text;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi
{
  public static class PsiExtensions
  {
    [CanBeNull]
    public static string GetPlainScalarText([CanBeNull] this INode node)
    {
      return (node as IPlainScalarNode)?.Text.GetText();
    }

    [CanBeNull]
    public static IBuffer GetPlainScalarTextBuffer([CanBeNull] this INode node)
    {
      return (node as IPlainScalarNode)?.Text.GetTextAsBuffer();
    }

    public static bool MatchesPlainScalarText([CanBeNull] this INode node, string value)
    {
      if (node is IPlainScalarNode scalar)
      {
        var bufferRange = new BufferRange(scalar.Text.GetTextAsBuffer(), new TextRange(0, scalar.Text.GetTextLength()));
        return bufferRange.StringEquals(value);
      }

      return false;
    }

    [CanBeNull]
    public static IBlockMappingEntry GetMapEntry([CanBeNull] this IBlockMappingNode mapNode, string simpleKey)
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

    [CanBeNull]
    public static IFlowMapEntry GetMapEntry([CanBeNull] this IFlowMappingNode mapNode, string simpleKey)
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

    [CanBeNull]
    public static T GetMapEntryValue<T>([CanBeNull] this IBlockMappingNode mapNode, string simpleKey)
      where T : class, INode
    {
      return mapNode.GetMapEntry(simpleKey)?.Content?.Value as T;
    }

    [CanBeNull]
    public static string GetMapEntryPlainScalarText([CanBeNull] this IBlockMappingNode mapNode, string simpleKey)
    {
      return mapNode.GetMapEntry(simpleKey)?.Content?.Value.GetPlainScalarText();
    }

    [CanBeNull]
    public static string GetMapEntryPlainScalarText([CanBeNull] this IFlowMappingNode mapNode, string simpleKey)
    {
      return mapNode.GetMapEntry(simpleKey)?.Value.GetPlainScalarText();
    }
  }
}