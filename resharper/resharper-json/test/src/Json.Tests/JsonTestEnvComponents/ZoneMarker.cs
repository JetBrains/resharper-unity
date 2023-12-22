using JetBrains.Application.BuildScript.Application.Zones;

namespace JetBrains.ReSharper.Plugins.Tests.JsonTestEnvComponents
{
    // All environment components must be in a host environment zone, such as the tests env zone, because they are the
    // only zones active before the environment container is composed. The normal test zone isn't activated until during
    // container composition, so any environment components in the normal test zone would already be filtered out. Keep
    // the environment components separate to shell/solution components, because they have different zone requirements.
    [ZoneMarker]
    public class ZoneMarker : IRequire<IJsonTestsEnvZone>
    {
    }
}
