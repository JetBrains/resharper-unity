using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services;
using JetBrains.ReSharper.Plugins.Json;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<ILanguageJsonNewZone>, IRequire<ICodeEditingZone>
    {
    }
}