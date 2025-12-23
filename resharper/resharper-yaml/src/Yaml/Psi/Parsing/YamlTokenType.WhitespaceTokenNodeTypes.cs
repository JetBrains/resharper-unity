using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing
{
  public partial class YamlTokenType
  {
    private sealed class WhitespaceNodeType : YamlTokenNodeType
    {
      public WhitespaceNodeType(int index)
        : base("WHITESPACE", index)
      {
      }

      public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
      {
        return new Whitespace(buffer, startOffset, endOffset);
      }

      public override bool IsFiltered => true;
      public override bool IsWhitespace => true;
      public override string TokenRepresentation => " ";
    }

    private sealed class NewLineNodeType : YamlTokenNodeType
    {
      public NewLineNodeType(int index)
        : base("NEW_LINE", index)
      {
      }

      public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
      {
        return new NewLine(buffer, startOffset, endOffset);
      }

      public override bool IsFiltered => true;
      public override bool IsWhitespace => true;
      public override string TokenRepresentation => @"\r\n";
    }
  }
}