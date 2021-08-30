using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew
{
    // Inherits activation from IPsiLanguageZone
    [ZoneDefinition]
    public interface ILanguageJsonNewZone : IPsiLanguageZone
    {
    }
}