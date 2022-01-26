using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Plugins.Yaml;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Yaml
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<ILanguageYamlZone>
    {
    }
}