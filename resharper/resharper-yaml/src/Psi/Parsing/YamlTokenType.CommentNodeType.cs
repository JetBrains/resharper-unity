using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing
{
  public static partial class YamlTokenType
  {
    private sealed class CommentTokenNodeType : YamlTokenNodeType
    {
      public CommentTokenNodeType(int index)
        : base("COMMENT", index)
      {
      }

      public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
      {
        return new Comment(buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
      }

      public override LeafElementBase Create(string token)
      {
        return new Comment(token);
      }

      // NOTE: Not filtered. This is because the spec only allows comments in certain places, so unlike e.g. C#, we let
      // the parser see comments
      public override bool IsComment => true;
      public override string TokenRepresentation => "# comment";
    }
  }
}