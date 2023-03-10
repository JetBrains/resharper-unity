using JetBrains.Application.BuildScript.Application.Zones;

namespace JetBrains.ReSharper.Plugins.Tests.Unity
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IUnityTestsZone>
    {
    }
}