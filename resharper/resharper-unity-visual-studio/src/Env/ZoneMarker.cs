using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.VsIntegration.Zones;

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio.Env
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IVisualStudioFrontendEnvZone>
    {
    }
}
