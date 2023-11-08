#nullable enable
using System;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Model;
using JetBrains.Rider.Model.Unity.FrontendBackend;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.HlslSupport.ShaderVariants;

public static class RdShaderPlatformEx
{
    public static ShaderPlatform AsShaderPlatform(this RdShaderPlatform value) => value switch
    {
        RdShaderPlatform.Desktop => ShaderPlatform.Desktop,
        RdShaderPlatform.Mobile => ShaderPlatform.Mobile,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
    
    public static RdShaderPlatform AsRdShaderPlatform(this ShaderPlatform value) => value switch
    {
        ShaderPlatform.Desktop => RdShaderPlatform.Desktop,
        ShaderPlatform.Mobile => RdShaderPlatform.Mobile,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}