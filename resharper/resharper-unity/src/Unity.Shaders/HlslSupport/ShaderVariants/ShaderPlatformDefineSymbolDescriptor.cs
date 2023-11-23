#nullable enable
using System;
using System.Collections.Immutable;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;

public class ShaderPlatformDefineSymbolDescriptor : IShaderDefineSymbolDescriptor
{
    public static readonly ShaderPlatformDefineSymbolDescriptor Instance = new();
    
    public const string Desktop = "SHADER_API_DESKTOP";
    public const string Mobile = "SHADER_API_MOBILE";

    public ImmutableArray<string> AllSymbols { get; } = ImmutableArray.Create<string>(Desktop, Mobile);

    public const ShaderPlatform DefaultValue = ShaderPlatform.Desktop;

    public bool IsDefaultSymbol(string defineSymbol) => defineSymbol == Desktop;

    public string GetDefineSymbol(ShaderPlatform shaderPlatform) =>
        shaderPlatform switch
        {
            ShaderPlatform.Desktop => Desktop,
            ShaderPlatform.Mobile => Mobile,
            _ => throw new ArgumentOutOfRangeException(nameof(shaderPlatform), shaderPlatform, null)
        };

    public ShaderPlatform GetValue(string defineSymbol) =>
        defineSymbol switch
        {
            Desktop => ShaderPlatform.Desktop,
            Mobile => ShaderPlatform.Mobile,
            _ => throw new ArgumentOutOfRangeException(nameof(defineSymbol), defineSymbol, null)
        };
}