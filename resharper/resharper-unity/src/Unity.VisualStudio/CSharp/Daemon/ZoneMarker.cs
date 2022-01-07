using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio.CSharp.Daemon
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<DaemonZone>
    {
    }
}