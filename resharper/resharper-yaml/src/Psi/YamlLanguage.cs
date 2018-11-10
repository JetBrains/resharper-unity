using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi
{
  [LanguageDefinition(Name)]
  public class YamlLanguage : KnownLanguage
  {
    public new const string Name = "YAML";

    [CanBeNull] public static readonly YamlLanguage Instance = null;

    public YamlLanguage()
      : base(Name, "YAML")
    {
    }
  }
}