using JetBrains.Application.BuildScript.Application.Zones;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests.UnityRider
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IRiderUnityTestsZone>
    {
    }
}