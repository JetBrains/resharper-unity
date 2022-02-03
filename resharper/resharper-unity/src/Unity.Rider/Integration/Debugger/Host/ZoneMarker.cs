using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Debugger.Host;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Debugger.Host
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IRiderDebuggerZone>
    {
    }
}