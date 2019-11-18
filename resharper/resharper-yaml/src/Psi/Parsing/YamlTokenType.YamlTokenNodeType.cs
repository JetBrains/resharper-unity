using System;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing
{
  public interface IYamlTokenNodeType : ITokenNodeType
  {
  }

  public static partial class YamlTokenType
  {
    public abstract class YamlTokenNodeType : TokenNodeType, IYamlTokenNodeType
    {
      protected YamlTokenNodeType(string s, int index)
        : base(s, index)
      {
      }

      public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
      {
        throw new InvalidOperationException($"TokenNodeType.Create needs to be overridden in {GetType()}");
      }

      public override bool IsFiltered => false;
      public override bool IsWhitespace => false; // this == WHITESPACE || this == NEW_LINE;
      public override bool IsComment => false;
      public override bool IsStringLiteral => false; // this == STRING_LITERAL
      public override bool IsConstantLiteral => false; // LITERALS[this]
      public override bool IsIdentifier => false; // this == IDENTIFIER
      public override bool IsKeyword => false; // KEYWORDS[this]
    }
  }
}