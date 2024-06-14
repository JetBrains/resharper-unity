using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Features.ReSpeller;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.ReSpeller
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IReSpellerZone>;
}