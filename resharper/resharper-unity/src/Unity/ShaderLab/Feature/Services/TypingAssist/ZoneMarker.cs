using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Feature.Services.TypingAssist
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<ILanguageCppZone>, IRequire<ICodeEditingZone>
  {
  }
}