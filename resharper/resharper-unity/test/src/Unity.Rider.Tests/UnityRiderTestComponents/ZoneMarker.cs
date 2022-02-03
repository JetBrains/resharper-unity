using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.TestFramework.Application.Zones;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests.UnityRiderTestComponents
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IRiderUnityTestsZone>, IRequire<ITestsEnvZone>
    {
    }
}
