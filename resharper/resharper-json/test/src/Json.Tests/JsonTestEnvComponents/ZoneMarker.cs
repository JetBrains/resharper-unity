using JetBrains.Application.BuildScript.Application.Zones;

namespace JetBrains.ReSharper.Plugins.Tests.JsonTestEnvComponents
{
    // Environment components must be defined with only the test env zone as a requirement. If a requirement is added for
    // a zone that is not active, the environment component will be filtered out. This is why environment components are
    // in a different namespace
    [ZoneMarker]
    public class ZoneMarker : IRequire<IJsonTestsEnvZone>
    {
    }
}