using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services.ExternalSources;
using JetBrains.ReSharper.Features.ReSpeller;
using JetBrains.ReSharper.Plugins.Json;
using JetBrains.ReSharper.Psi.CSharp;

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio.Core.Feature.Respeller
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IReSpellerZone>, IRequire<ExternalSourcesZone>, IRequire<ILanguageCSharpZone>, IRequire<ILanguageJsonNewZone>
    {
    }
}