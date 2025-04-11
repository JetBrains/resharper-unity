using JetBrains.Application.Environment;
using JetBrains.VsIntegration.Env;

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio.InProcess
{
    [ZoneActivator]
    public class ZoneActivator : IActivateDynamic<IUnityPluginZone>, IActivateDynamic<IUnityShaderZone>
    {
        private readonly VisualStudioProtocolConnector myConnector;

        public ZoneActivator(VisualStudioProtocolConnector connector)
        {
            myConnector = connector;
        }

        bool IActivateDynamic<IUnityPluginZone>.ActivatorEnabled() => !myConnector.IsOutOfProcess.Value;
        bool IActivateDynamic<IUnityShaderZone>.ActivatorEnabled() => !myConnector.IsOutOfProcess.Value;
    }
}