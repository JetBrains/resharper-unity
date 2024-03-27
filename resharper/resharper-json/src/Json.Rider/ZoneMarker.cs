using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.RdBackend.Common.Env;

namespace JetBrains.ReSharper.Plugins.Json.Rider
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<ILanguageJsonNewZone>, IRequire<IReSharperHostCoreFeatureZone>
    {
    }
}