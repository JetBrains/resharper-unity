using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Features.ReSpeller;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.ReSpeller
{
    [ZoneMarker]
    public class ZoneMarker: IRequire<IReSpellerZone>
    {
    }
}