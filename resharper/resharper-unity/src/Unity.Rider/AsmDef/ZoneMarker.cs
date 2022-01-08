using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Plugins.Json;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.AsmDef
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<ILanguageJsonNewZone>
    {
    }
}