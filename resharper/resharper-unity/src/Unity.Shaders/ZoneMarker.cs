using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.ExternalSources;
using JetBrains.ReSharper.Plugins.Json;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IUnityPluginZone>, IRequire<ILanguageCppZone>
    {
    }
}