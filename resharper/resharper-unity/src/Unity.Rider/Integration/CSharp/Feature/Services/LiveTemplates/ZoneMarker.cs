using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.CSharp.Feature.Services.LiveTemplates
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<ICodeEditingZone>
    {
    }
}