using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Debugger.Host;

namespace JetBrains.ReSharper.Plugins.Unity.Debugger.Host.Rider
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IRiderDebuggerZone>
    {
    }
}