using JetBrains.Application.Environment;
using JetBrains.VsIntegration.Env;

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio.InProcess
{
    [ZoneActivator]
    public class ZoneActivator : IActivateDynamic<IUnityPluginZone>, IActivateDynamic<IUnityShaderZone>
    {
        private readonly VisualStudioOutOfProcessMode myOopMode;

        public ZoneActivator(VisualStudioOutOfProcessMode oopMode)
        {
            myOopMode = oopMode;
        }

        bool IActivateDynamic<IUnityPluginZone>.ActivatorEnabled() => !myOopMode.IsOutOfProcess;
        bool IActivateDynamic<IUnityShaderZone>.ActivatorEnabled() => !myOopMode.IsOutOfProcess;
    }
}