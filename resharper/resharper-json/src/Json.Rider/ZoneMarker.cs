using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.RdBackend.Common.Env;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.Json.Rider
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<ILanguageJsonNewZone>,
        IRequire<PsiFeaturesImplZone>,
        IRequire<IResharperHostCoreFeatureZone>
    {
    }
}