using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Rider.Backend.Env;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IRiderPlatformZone>
    {
    }
}