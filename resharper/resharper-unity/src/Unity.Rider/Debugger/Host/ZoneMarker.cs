using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Debugger.Host;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Host
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IRiderDebuggerZone>
    {
    }
}