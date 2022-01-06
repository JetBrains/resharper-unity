using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Features.ReSpeller;

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio.Core.Feature.Respeller
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IReSpellerZone>
    {
    }
}