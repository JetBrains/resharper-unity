using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Yaml
{
  // Inherits activation from IPsiLanguageZone
  [ZoneDefinition]
  public interface ILanguageYamlZone : IPsiLanguageZone
  {
  }
}