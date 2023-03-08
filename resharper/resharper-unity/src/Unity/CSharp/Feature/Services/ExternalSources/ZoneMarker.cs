using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services.ExternalSources;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ExternalSources
{
    // Not strictly necessary. We implement IExternalSourcesDefinesProvider, which will only be consumed if
    // ExternalSourcesZone is active. If it's not active, it doesn't matter if our component is active or not.
    [ZoneMarker]
    public class ZoneMarker : IRequire<ExternalSourcesZone>
    {
    }
}