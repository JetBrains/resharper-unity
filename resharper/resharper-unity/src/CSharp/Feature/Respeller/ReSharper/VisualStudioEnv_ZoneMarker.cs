using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.VsIntegration.Shell.Zones;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Respeller
{
    // ReSharper disable once InconsistentNaming
    [ZoneMarker]
    public class VisualStudioEnv_ZoneMarker : IRequire<IVisualStudioEnvZone>
    {
    }
}