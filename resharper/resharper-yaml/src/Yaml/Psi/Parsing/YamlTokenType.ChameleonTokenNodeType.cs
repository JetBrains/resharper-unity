using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing
{
  public partial class YamlTokenType
  {
    internal sealed class ChameleonTokenNodeType : YamlTokenNodeType
    {
      public readonly int LexerIndent;

      public ChameleonTokenNodeType(string s, int lexerIndent, int index, string representation)
        : base(s, index)
      {
        LexerIndent = lexerIndent;
        TokenRepresentation = representation;
      }

      public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
      {
        return new ClosedChameleonElement(YamlTokenType.GetChameleonMapEntryValueWithIndent(LexerIndent), endOffset.Offset - startOffset.Offset);
      }

      public override string TokenRepresentation { get; }

      public override bool Equals(object obj)
      {
        if (obj is ChameleonTokenNodeType tokeType)
          return Equals(tokeType);

        return false;
      }

      private bool Equals(ChameleonTokenNodeType other)
      {
        return other.Index == Index;
      }

      public override int GetHashCode()
      {
        return Index;
      }
    }

  }
}