using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Feature.Services.CodeCompletion
{
    [ZoneMarker]
    public class ZoneMarker: IRequire<ICodeEditingZone>
    {
    }
}