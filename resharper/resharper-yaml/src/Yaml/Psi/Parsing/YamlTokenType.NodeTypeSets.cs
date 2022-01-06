using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing
{
  public static partial class YamlTokenType
  {
    public static readonly NodeTypeSet COMMENTS_OR_WHITE_SPACES;

    static YamlTokenType()
    {
      COMMENTS_OR_WHITE_SPACES = new NodeTypeSet(NEW_LINE, WHITESPACE, COMMENT);
    }
  }
}
