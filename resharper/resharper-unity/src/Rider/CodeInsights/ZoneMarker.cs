using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Host.Env;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IRiderPlatformZone>
    {
    }
}