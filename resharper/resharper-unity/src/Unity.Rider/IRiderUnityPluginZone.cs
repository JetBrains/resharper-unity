using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Rider.Backend.Env;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [ZoneDefinition]
    public interface IRiderUnityPluginZone : IZone, IRequire<IRiderPlatformZone>, IRequire<IUnityPluginZone>
    {
    }
}