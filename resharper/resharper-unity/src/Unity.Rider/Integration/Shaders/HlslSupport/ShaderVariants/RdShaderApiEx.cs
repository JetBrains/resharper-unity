#nullable enable
using System;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Model;
using JetBrains.Rider.Model.Unity.FrontendBackend;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.HlslSupport.ShaderVariants;

public static class RdShaderApiEx
{
    public static ShaderApi AsShaderApi(this RdShaderApi rdShaderApi) => rdShaderApi switch
    {
        RdShaderApi.D3D11 => ShaderApi.D3D11,
        RdShaderApi.GlCore => ShaderApi.GlCore,
        RdShaderApi.GlEs => ShaderApi.GlEs,
        RdShaderApi.GlEs3 => ShaderApi.GlEs3,
        RdShaderApi.Metal => ShaderApi.Metal,
        RdShaderApi.Vulkan => ShaderApi.Vulkan,
        RdShaderApi.D3D11L9X => ShaderApi.D3D11L9X,
        _ => throw new ArgumentOutOfRangeException(nameof(rdShaderApi), rdShaderApi, null)
    };
    
    public static RdShaderApi AsRdShaderApi(this ShaderApi shaderApi) => shaderApi switch
    {
        ShaderApi.D3D11 => RdShaderApi.D3D11,
        ShaderApi.GlCore => RdShaderApi.GlCore,
        ShaderApi.GlEs => RdShaderApi.GlEs,
        ShaderApi.GlEs3 => RdShaderApi.GlEs3,
        ShaderApi.Metal => RdShaderApi.Metal,
        ShaderApi.Vulkan => RdShaderApi.Vulkan,
        ShaderApi.D3D11L9X => RdShaderApi.D3D11L9X,
        _ => throw new ArgumentOutOfRangeException(nameof(shaderApi), shaderApi, null)
    };
}