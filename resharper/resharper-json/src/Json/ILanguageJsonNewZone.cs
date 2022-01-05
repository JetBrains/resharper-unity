using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Json
{
    // Inherits activation from IPsiLanguageZone
    [ZoneDefinition]
    public interface ILanguageJsonNewZone : IPsiLanguageZone
    {
    }
}