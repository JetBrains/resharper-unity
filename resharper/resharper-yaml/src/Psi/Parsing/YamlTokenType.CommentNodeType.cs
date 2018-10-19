using System;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
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

      // NOTE: Not filtered
      public override bool IsComment => true;
      public override string TokenRepresentation => "# comment";
    }
  }
}