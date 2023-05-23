using JetBrains.Application.Environment;
using JetBrains.ReSharper.Plugins.Unity.Shaders;

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio.Env
{
    [ZoneActivator]
    public class ZoneActivator : IActivate<IUnityPluginZone>, IActivate<IUnityShaderZone>
    {
    }
}