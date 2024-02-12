using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Rider.Backend.Env;
using JetBrains.Rider.Backend.Product;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration
{
    // Everything in the Integration namespace requires a full Rider environment, including the protocol. This isn't
    // available in tests, so we have to separate it from Common, which includes components and functionality that also
    // work in tests. This zone sets up a requirement on Rider's environment zone, which disables it during testing.
    [ZoneMarker]
    public class ZoneMarker : IRequire<IRiderProductEnvironmentZone>,
        IRequire<IRiderFeatureZone>,
        IRequire<IRiderUnityPluginZone>
    {
    }
}