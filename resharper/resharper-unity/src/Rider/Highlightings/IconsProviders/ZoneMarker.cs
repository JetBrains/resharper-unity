using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Host.Core.Env;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IRiderPlatformZone>
    {
    }
}