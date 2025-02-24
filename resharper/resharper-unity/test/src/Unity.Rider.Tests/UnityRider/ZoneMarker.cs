using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Plugins.Unity;
using JetBrains.ReSharper.TestFramework;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests.UnityRider
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IRiderUnityTestsZone>
    {
    }
}

namespace JetBrains.ReSharper.Plugins.Tests.Unity
{
    [ZoneMarker]
    public class ZoneMarker : 
        IRequire<PsiFeatureTestZone>,
        IRequire<IUnityPluginZone>,
        IRequire<IUnityShaderZone>
    {
    }
}