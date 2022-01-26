using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.RdBackend.Common.Env;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.ExternalSources;
using JetBrains.ReSharper.Plugins.Json;
using JetBrains.ReSharper.Plugins.Yaml;
using JetBrains.ReSharper.Psi;
using JetBrains.Rider.Backend.Env;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    // Various features require IRiderProductEnvironmentZone, but we can't seem to use that in tests
    // Same with IRiderFeatureZone

    [ZoneDefinition]
    public interface IRiderUnityPluginZone : IZone, IRequire<IRiderPlatformZone>, IRequire<ILanguageCppZone>, IRequire<IUnityPluginZone>
    {
        
    }
    
    [ZoneMarker]
    public class ZoneMarker : IRequire<IRiderUnityPluginZone>
    {
    }
}