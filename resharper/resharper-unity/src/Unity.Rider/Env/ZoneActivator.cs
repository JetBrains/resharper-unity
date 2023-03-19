using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Application.Environment;
using JetBrains.ReSharper.Plugins.Unity.Shaders;
using JetBrains.Rider.Backend.Product;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Env
{
    [ZoneMarker, ZoneActivator]
    public class ZoneActivator : IRequire<IRiderProductEnvironmentZone>,
        IActivate<IRiderUnityPluginZone>,
        IActivate<IUnityShaderZone>
    {
    }
}