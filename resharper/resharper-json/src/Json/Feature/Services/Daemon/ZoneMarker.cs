using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace JetBrains.ReSharper.Plugins.Json.Feature.Services.Daemon;

[ZoneMarker]
public class ZoneMarker : IRequire<DaemonZone>;
