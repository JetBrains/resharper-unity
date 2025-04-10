using JetBrains.Application.Environment;
using JetBrains.VsIntegration.Env;

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio.InProcess
{
    [ZoneActivator]
    public class ZoneActivator(VisualStudioProtocolConnector connector) : IActivateDynamic<IUnityPluginZone>, IActivateDynamic<IUnityShaderZone>
    {
        bool IActivateDynamic<IUnityPluginZone>.ActivatorEnabled() => !connector.IsOutOfProcess.Value;
        bool IActivateDynamic<IUnityShaderZone>.ActivatorEnabled() => !connector.IsOutOfProcess.Value;
    }
}