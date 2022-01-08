using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Rider.Backend.Env;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.CSharp.Daemon.CodeInsights
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IRiderPlatformZone>
    {
    }
}