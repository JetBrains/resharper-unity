using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi
{
  [LanguageDefinition(Name)]
  public class UALanguage : KnownLanguage
  {
    public new const string Name = "UA";

    [CanBeNull] public static readonly UALanguage Instance = null;

    public UALanguage()
      : base(Name, "UA")
    {
    }
  }
}