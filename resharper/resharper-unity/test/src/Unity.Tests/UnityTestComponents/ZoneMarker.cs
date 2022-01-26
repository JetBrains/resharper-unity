using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Plugins.Unity;
using JetBrains.TestFramework.Application.Zones;

namespace JetBrains.ReSharper.Plugins.Tests.UnityTestComponents
{
    // Zone requirements for non-environment test components. Separate namespace to environment components to avoid
    // adding inactive zones as requirements to environment components
    [ZoneMarker]
    public class ZoneMarker : IRequire<IRiderUnityTestsZone>, IRequire<ITestsEnvZone>
    {
    }
}
