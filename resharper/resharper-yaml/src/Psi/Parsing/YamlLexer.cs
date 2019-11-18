using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing
{
  public class YamlLexer : YamlLexerGenerated
  {
    public YamlLexer(IBuffer buffer, bool allowChameleonOptimizations, bool atChameleonStart)
      : base(buffer, allowChameleonOptimizations)
    {
      AtChameleonStart = atChameleonStart;
      if (AtChameleonStart)
        InitialLexicalState = BLOCK;
    }
  }
}