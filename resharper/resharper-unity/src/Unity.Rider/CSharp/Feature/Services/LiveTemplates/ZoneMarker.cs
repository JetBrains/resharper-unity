using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.CSharp.Feature.Services.LiveTemplates
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<ICodeEditingZone>
    {
    }
}