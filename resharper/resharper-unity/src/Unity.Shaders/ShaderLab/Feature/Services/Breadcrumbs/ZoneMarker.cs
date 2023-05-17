using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services.Breadcrumbs;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.Breadcrumbs
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IBreadcrumbsZone>
    {
    }
}