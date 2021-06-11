using JetBrains.Application.BuildScript.Application.Zones;
 using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<ILanguageHlslSupportZone>
  {
  }

  // By inheriting from ILanguageCppZone, activation is also inherited
  [ZoneDefinition]
  public interface ILanguageHlslSupportZone : ILanguageCppZone
  {
  }
}