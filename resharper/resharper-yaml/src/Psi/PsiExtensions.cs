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
    public static IBlockMappingEntry FindMapEntryBySimpleKey([CanBeNull] this IBlockMappingNode mapNode, string keyName)
    {
      if (mapNode == null)
        return null;

      foreach (var mappingEntry in mapNode.EntriesEnumerable)
      {
        if (mappingEntry.Key.MatchesPlainScalarText(keyName))
          return mappingEntry;
      }

      return null;
    }

    [CanBeNull]
    public static IFlowMapEntry FindMapEntryBySimpleKey([CanBeNull] this IFlowMappingNode mapNode, string keyName)
    {
      if (mapNode == null)
        return null;

      foreach (var sequenceEntry in mapNode.EntriesEnumerable)
      {
        if (sequenceEntry.Key.MatchesPlainScalarText(keyName))
          return sequenceEntry;
      }

      return null;
    }

    [CanBeNull]
    public static T GetSimpleMapEntryValue<T>([CanBeNull] this IBlockMappingNode mapNode, string keyName)
      where T : class, INode
    {
      return mapNode.FindMapEntryBySimpleKey(keyName)?.Content?.Value as T;
    }

    [CanBeNull]
    public static string GetSimpleMapEntryPlainScalarText([CanBeNull] this IBlockMappingNode mapNode, string keyName)
    {
      return mapNode.FindMapEntryBySimpleKey(keyName)?.Content?.Value.GetPlainScalarText();
    }

    [CanBeNull]
    public static string GetSimpleMapEntryPlainScalarText([CanBeNull] this IFlowMappingNode mapNode, string keyName)
    {
      return mapNode.FindMapEntryBySimpleKey(keyName)?.Value.GetPlainScalarText();
    }
  }
}