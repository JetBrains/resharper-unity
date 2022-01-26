using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Rider.Backend.Env;
using JetBrains.Rider.Backend.Product;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IRiderProductEnvironmentZone>, IRequire<IRiderFeatureZone>
    {
        
    }
}