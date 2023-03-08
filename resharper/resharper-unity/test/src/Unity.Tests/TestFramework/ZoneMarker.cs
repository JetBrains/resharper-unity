using JetBrains.Application.BuildScript.Application.Zones;

namespace JetBrains.ReSharper.Plugins.Tests.TestFramework
{
    // Zone requirements for the test framework base classes in this namespace. Typically, there aren't any components,
    // but the classes can use components via GetComponent<T>
    [ZoneMarker]
    public class ZoneMarker : IRequire<IUnityTestsZone>
    {
    }
}