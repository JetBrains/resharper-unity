using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Psi.CSharp;

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio.CSharp
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<ILanguageCSharpZone>
    {
    }
}