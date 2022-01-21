using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.RdBackend.Common.Env;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.Rider.Backend.Env;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    // Various features require IRiderProductEnvironmentZone, but we can't seem to use that in tests
    // Same with IRiderFeatureZone
    [ZoneMarker]
    public class ZoneMarker : IRequire<IRiderPlatformZone>,
            IRequire<DaemonZone>,
            IRequire<IResharperHostCoreFeatureZone>
        //, IRequire<IRiderProductEnvironmentZone>
    {
    }
}