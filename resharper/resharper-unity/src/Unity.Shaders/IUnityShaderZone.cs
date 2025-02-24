using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Psi;

// NOTE: The namespace intentionally does not contain ".Shaders" part so that IUnityShaderZone is not marked by IUnityPluginZone (from the root ZoneMarker).
// ReSharper disable CheckNamespace
namespace JetBrains.ReSharper.Plugins.Unity
{
    [ZoneDefinition]
    public interface IUnityShaderZone : IZone, IRequire<ILanguageCppZone>
    {
    }
}