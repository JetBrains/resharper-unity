using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing
{
  public class YamlFilteringLexer : FilteringLexer, ILexer<int>
  {
    public YamlFilteringLexer([NotNull] ILexer lexer)
      : base(lexer)
    {
    }

    protected override bool Skip(TokenNodeType tokenType)
    {
      return tokenType.IsFiltered;
    }

    int ILexer<int>.CurrentPosition
    {
      get => ((ILexer<int>) myLexer).CurrentPosition;
      set => ((ILexer<int>) myLexer).CurrentPosition = value;
    }
  }
}