using JetBrains.Application.BuildScript.Application.Zones;

namespace JetBrains.ReSharper.Plugins.Json
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<ILanguageJsonNewZone>
    {
    }
}