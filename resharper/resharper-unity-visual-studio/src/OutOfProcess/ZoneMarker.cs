using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Platform.VisualStudio.AnyEnd.BuildScript;

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio.OutOfProcess
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IVisualStudioBackendOutOfProcessEnvZone>
    {
    }
}
