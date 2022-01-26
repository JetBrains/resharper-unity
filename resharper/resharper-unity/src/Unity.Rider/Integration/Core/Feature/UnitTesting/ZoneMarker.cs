using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.UnitTestFramework;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.UnitTesting
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IUnitTestingZone>
    {
    }
}