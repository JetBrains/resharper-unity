using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.VsIntegration.Shell.Zones;

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio
{
    // TODO: Should this have a VS specific zone requirement? Which one? How do we activate it for testing?
    // If we add one, we can't use this assembly in tests (because e.g. IVisualStudioFrontendEnvZone expects Visual
    // Studio). If we don't add one, then we must rely on assembly location to prevent loading in the wrong environment
    [ZoneMarker]
    public class ZoneMarker: IRequire<IVisualStudioFrontendEnvZone>, IRequire<IUnityPluginZone>
    {
    }
}