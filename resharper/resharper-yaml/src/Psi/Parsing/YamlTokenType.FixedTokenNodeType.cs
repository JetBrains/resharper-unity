using System;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing
{
  public static partial class YamlTokenType
  {
    // A fixed length token node type, e.g. keyword, symbol, etc.
    private sealed class FixedTokenNodeType : YamlTokenNodeType
    {
      public FixedTokenNodeType(string s, int index, string representation)
        : base(s, index)
      {
        TokenRepresentation = representation;
      }

      public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
      {
        return new FixedTokenElement(this);
      }

      public override string TokenRepresentation { get; }
    }

    public class FixedTokenElement : YamlTokenBase
    {
      private readonly TokenNodeType myTokenNodeType;

      public FixedTokenElement(TokenNodeType tokenNodeType)
      {
        myTokenNodeType = tokenNodeType;
      }

      public override int GetTextLength() => myTokenNodeType.TokenRepresentation.Length;
      public override string GetText() => myTokenNodeType.TokenRepresentation;
      public override NodeType NodeType => myTokenNodeType;
    }
  }
}