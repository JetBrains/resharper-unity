using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi
{
  [LanguageDefinition(Name)]
  public class YamlLanguage : KnownLanguage
  {
    public new const string Name = "YAML";

    [CanBeNull, UsedImplicitly]
    public static YamlLanguage Instance { get; private set; }

    public YamlLanguage() : base(Name, "YAML")
    {
    }
    
    protected YamlLanguage([NotNull] string name, [NotNull] string presentableName) : base(name, presentableName)
    {
    }
  }
}
