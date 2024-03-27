using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.RdBackend.Common.Env;

namespace JetBrains.ReSharper.Plugins.Yaml.Rider
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<ILanguageYamlZone>, IRequire<IReSharperHostCoreFeatureZone>
    {
    }
}