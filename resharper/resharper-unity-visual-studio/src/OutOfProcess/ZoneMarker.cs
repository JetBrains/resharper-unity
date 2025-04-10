using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Platform.VisualStudio.Protocol.BuildScript;

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio.OutOfProcess
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IVisualStudioBackendOutOfProcessEnvZone>
    {
    }
}
