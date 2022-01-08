using JetBrains.Application.BuildScript.Application.Zones;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Core.Feature.UnitTesting
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IUnityUnitTestingZone>
    {
    }
}