using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Plugins.Unity.JsonNew;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<ILanguageJsonNewZone>
    {
    }
}