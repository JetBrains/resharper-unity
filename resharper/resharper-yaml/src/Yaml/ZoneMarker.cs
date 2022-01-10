using JetBrains.Application.BuildScript.Application.Zones;

namespace JetBrains.ReSharper.Plugins.Yaml
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<ILanguageYamlZone>
  {
  }
}