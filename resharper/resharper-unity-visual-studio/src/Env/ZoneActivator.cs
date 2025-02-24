using JetBrains.Application.Environment;

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio.Env
{
    [ZoneActivator]
    public class ZoneActivator : IActivate<IUnityPluginZone>, IActivate<IUnityShaderZone>
    {
    }
}