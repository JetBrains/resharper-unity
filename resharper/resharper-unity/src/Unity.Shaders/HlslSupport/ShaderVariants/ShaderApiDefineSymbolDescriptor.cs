#nullable enable
using System;
using System.Collections.Immutable;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;

public class ShaderApiDefineSymbolDescriptor : IShaderDefineSymbolDescriptor
{
    public static readonly ShaderApiDefineSymbolDescriptor Instance = new();
    
    public const string D3D11 = "SHADER_API_D3D11";
    public const string GlCore = "SHADER_API_GLCORE";
    public const string GlEs = "SHADER_API_GLES";
    public const string GlEs3 = "SHADER_API_GLES3";
    public const string Metal = "SHADER_API_METAL";
    public const string Vulkan = "SHADER_API_VULKAN";
    public const string D3D11L9X = "SHADER_API_D3D11_9X";
    public const string Desktop = "SHADER_API_DESKTOP";
    public const string Mobile = "SHADER_API_MOBILE";

    public ImmutableArray<string> AllSymbols = ImmutableArray.Create<string>(D3D11, GlCore, GlEs, GlEs3, Metal, Vulkan, D3D11L9X, Desktop, Mobile);  

    public string GetDefineSymbol(ShaderApi shaderApi) =>
        shaderApi switch
        {
            ShaderApi.D3D11 => D3D11,
            ShaderApi.GlCore => GlCore,
            ShaderApi.GlEs => GlEs,
            ShaderApi.GlEs3 => GlEs3,
            ShaderApi.Metal => Metal,
            ShaderApi.Vulkan => Vulkan,
            ShaderApi.D3D11L9X => D3D11L9X,
            ShaderApi.Desktop => Desktop,
            ShaderApi.Mobile => Mobile,
            _ => throw new ArgumentOutOfRangeException(nameof(shaderApi), shaderApi, null)
        };
}