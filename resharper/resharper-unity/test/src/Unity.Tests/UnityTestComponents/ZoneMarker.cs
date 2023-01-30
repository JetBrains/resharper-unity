using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.TestFramework.Application.Zones;

namespace JetBrains.ReSharper.Plugins.Tests.UnityTestComponents
{
    // Zone requirements for non-environment test components. Separate namespace to environment components to avoid
    // adding inactive zones as requirements to environment components
    [ZoneMarker]
    public class ZoneMarker : IRequire<IUnityTestsZone>, IRequire<ITestsEnvZone>
    {
    }
}
