#nullable enable
using System.Net;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.RenderDoc;

public class RenderDocConfig
{
    public IPAddress HostAddress { get; init; } = IPAddress.Loopback;
}