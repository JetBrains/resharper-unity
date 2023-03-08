using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders
{
    [ZoneDefinition]
    public interface IUnityShaderZone : IZone, IRequire<IUnityPluginZone>, IRequire<ILanguageCppZone>
    {
    }
}