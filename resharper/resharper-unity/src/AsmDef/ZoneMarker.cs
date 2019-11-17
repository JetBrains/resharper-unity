using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Psi.JavaScript;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<ILanguageJavaScriptZone>
    {
    }
}