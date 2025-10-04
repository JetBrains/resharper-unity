using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Application.Environment;
using JetBrains.Rider.Backend.Product;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Env
{
    [ZoneMarker, ZoneActivator]
    public class ZoneActivator : IRequire<IRiderProductFullEnvironmentZone>,
        IActivate<IRiderUnityPluginZone>,
        IActivate<IUnityShaderZone>,
        IActivate<IUnityPluginZone>
    {
    }
}