using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing
{
  public partial class YamlTokenType
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
        return new GenericTokenElement(this, buffer, startOffset, endOffset);
      }

      public override string TokenRepresentation { get; }
    }

    public class GenericTokenElement : YamlTokenBase
    {
      public GenericTokenElement(TokenNodeType tokenNodeType, IBuffer buffer, TreeOffset startOffset,
                                 TreeOffset endOffset)
        : base(tokenNodeType, buffer, startOffset, endOffset)
      {
      }
    }
    
  }
}