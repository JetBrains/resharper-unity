using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Rider.Backend.Env;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IRiderPlatformZone>
    {
    }
}