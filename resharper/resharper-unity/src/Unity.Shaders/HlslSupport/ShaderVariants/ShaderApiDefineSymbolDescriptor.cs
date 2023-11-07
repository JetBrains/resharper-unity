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

    public ImmutableArray<string> AllSymbols { get; } = ImmutableArray.Create<string>(D3D11, GlCore, GlEs, GlEs3, Metal, Vulkan, D3D11L9X);

    public const ShaderApi DefaultValue = ShaderApi.D3D11;

    public bool IsDefaultSymbol(string defineSymbol) => defineSymbol == D3D11;

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
            _ => throw new ArgumentOutOfRangeException(nameof(shaderApi), shaderApi, null)
        };

    public ShaderApi GetValue(string defineSymbol) =>
        defineSymbol switch
        {
            D3D11 => ShaderApi.D3D11,
            GlCore => ShaderApi.GlCore,
            GlEs => ShaderApi.GlEs,
            GlEs3 => ShaderApi.GlEs3,
            Metal => ShaderApi.Metal,
            Vulkan => ShaderApi.Vulkan,
            D3D11L9X => ShaderApi.D3D11L9X,
            _ => throw new ArgumentOutOfRangeException(nameof(defineSymbol), defineSymbol, null)
        };
}