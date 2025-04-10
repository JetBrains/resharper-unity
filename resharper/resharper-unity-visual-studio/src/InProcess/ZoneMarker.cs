using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.VsIntegration.Zones;

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio.InProcess
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IVisualStudioFrontendEnvZone>
    {
    }
}
