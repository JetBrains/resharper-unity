using JetBrains.Application.BuildScript.Application.Zones;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests.UnityRiderTestComponents
{
    // Zone requirements for all shell and solution test components. Make sure to restrict this to the plugin under
    // test, or the components will leak into the full product when built from the monorepo. Environment components must
    // be in another namespace, as the tests zone is not yet activated when the environment container is composed.
    // This marker has to be in a separate namespace to TestEnvironment.cs, or the requirement would also apply and
    // filter out the activator, preventing activation.
    [ZoneMarker]
    public class ZoneMarker : IRequire<IRiderUnityTestsZone>
    {
    }
}
