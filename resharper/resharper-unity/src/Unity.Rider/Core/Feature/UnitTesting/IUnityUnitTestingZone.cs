using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.Rider.Backend.Env;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Core.Feature.UnitTesting
{
    [ZoneDefinition]
    [ZoneDefinitionConfigurableFeature("Exploration for UnityTests", "Support for [UnityTest] tests", false)]
    public interface IUnityUnitTestingZone : IZone, IRequire<IUnitTestingZone>, IRequire<IRiderPlatformZone>
    {
    }
}