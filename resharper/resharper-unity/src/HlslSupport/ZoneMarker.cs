using JetBrains.Application.BuildScript.Application.Zones;
 using JetBrains.ReSharper.Psi;
 using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport
{
  [ZoneMarker]
  public class ZoneMarker :
    IRequire<ILanguageCppZone>
  {
  }
}