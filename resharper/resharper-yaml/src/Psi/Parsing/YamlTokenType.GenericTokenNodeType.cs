using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing
{
  public static partial class YamlTokenType
  {
    private sealed class GenericTokenNodeType : YamlTokenNodeType
    {
      public GenericTokenNodeType(string s, int index, string representation)
        : base(s, index)
      {
        TokenRepresentation = representation;
      }

      public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
      {
        return new GenericTokenElement(this, buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
      }

      public override string TokenRepresentation { get; }
    }

    public class GenericTokenElement : YamlTokenBase
    {
      private readonly TokenNodeType myTokenNodeType;
      private readonly string myText;

      public GenericTokenElement(TokenNodeType tokenNodeType, string text)
      {
        myTokenNodeType = tokenNodeType;
        myText = text;
      }

      public override int GetTextLength() => myText.Length;
      public override string GetText() => myText;
      public override NodeType NodeType => myTokenNodeType;
    }
  }
}