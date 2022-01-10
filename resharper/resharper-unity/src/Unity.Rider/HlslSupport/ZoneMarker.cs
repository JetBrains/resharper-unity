using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Plugins.Unity.HlslSupport;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.HlslSupport
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<ILanguageHlslSupportZone>
    {
    }
}