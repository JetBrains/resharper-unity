using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Features.ReSpeller;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Respeller
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IReSpellerZone>
    {
    }
}