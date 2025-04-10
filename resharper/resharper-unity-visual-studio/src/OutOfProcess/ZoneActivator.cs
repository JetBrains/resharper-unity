using JetBrains.Application.Environment;

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio.OutOfProcess
{
    [ZoneActivator]
    public class ZoneActivator : IActivate<IUnityPluginZone>, IActivate<IUnityShaderZone>
    {
    }
}