using JetBrains.Application.BuildScript.Application.Zones;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.Shaders;

[ZoneMarker]
public class ZoneMarker : IRequire<IUnityShaderZone>, IRequire<IUnityPluginZone>;