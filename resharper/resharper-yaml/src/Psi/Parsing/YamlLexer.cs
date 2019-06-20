using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing
{
  public class YamlLexer : YamlLexerGenerated
  {
    public YamlLexer(IBuffer buffer, bool atChameleonStart = false)
      : base(buffer)
    {
      AtChameleonStart = atChameleonStart;
      if (AtChameleonStart)
        InitialLexicalState = BLOCK;
    }
  }
}