using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.RdBackend.Common.Env;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IRiderPlatformZone>
    {
    }
}