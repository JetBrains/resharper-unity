using JetBrains.Application.BuildScript.Application.Zones;

namespace JetBrains.ReSharper.Plugins.Tests.Yaml
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<IYamlTestsZone>
  {
  }
}