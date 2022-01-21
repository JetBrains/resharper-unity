using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.ExternalSources;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Plugins.Json;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [ZoneMarker]
    public class ZoneMarker : 
        IRequire<DaemonZone>, 
        IRequire<NavigationZone>, 
        IRequire<ICodeEditingZone>, 
        IRequire<ExternalSourcesZone>,
        IRequire<ILanguageJsonNewZone>
    {
    }
}