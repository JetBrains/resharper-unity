using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Rider.Backend.Env;
using JetBrains.Rider.Backend.Product;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.Profiler
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IRiderFullFeatureZone>, IRequire<IRiderProductFullEnvironmentZone>
    {
    }
}