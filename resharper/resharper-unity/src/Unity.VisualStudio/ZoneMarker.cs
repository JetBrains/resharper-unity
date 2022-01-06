using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.VsIntegration.Shell.Zones;

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IVisualStudioFrontendEnvZone>
    {
    }
}