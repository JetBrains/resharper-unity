using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services.ExternalSources;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.ExternalSources
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<ExternalSourcesZone>
    {
    }
}