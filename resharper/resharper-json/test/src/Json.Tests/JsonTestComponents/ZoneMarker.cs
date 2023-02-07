using JetBrains.Application.BuildScript.Application.Zones;

namespace JetBrains.ReSharper.Plugins.Tests.JsonTestComponents
{
    // Zone requirements for all test components (environment, shell, solution, etc.). Make sure to restrict this to the
    // plugin under test, or the components will leak out into the full product when built in the monorepo.
    // This has to be in a separate namespace to TestEnvironment.cs, or the requirement would also apply and filter out
    // the activator, preventing activation.
    [ZoneMarker]
    public class ZoneMarker : IRequire<IJsonTestsZone>
    {
    }
}