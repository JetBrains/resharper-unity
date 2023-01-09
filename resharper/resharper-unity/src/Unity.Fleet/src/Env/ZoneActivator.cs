using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Application.Environment;
using JetBrains.Fleet.Backend.Env;

namespace JetBrains.ReSharper.Plugins.Unity.Fleet.Env
{
    [ZoneMarker(typeof(IFleetFeatureEnvironmentZone))]
    [ZoneActivator]
    public class ZoneActivator : IActivate<IUnityPluginZone>
    {
    }
}