using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing
{
  public partial class YamlTokenType
  {
    private sealed class CommentTokenNodeType : YamlTokenNodeType
    {
      public CommentTokenNodeType(int index)
        : base("COMMENT", index)
      {
      }

      public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
      {
        return new Comment(buffer, startOffset, endOffset);
      }

      // NOTE: Not filtered. This is because the spec only allows comments in certain places, so unlike e.g. C#, we let
      // the parser see comments
      public override bool IsComment => true;
      public override string TokenRepresentation => "# comment";
    }
  }
}