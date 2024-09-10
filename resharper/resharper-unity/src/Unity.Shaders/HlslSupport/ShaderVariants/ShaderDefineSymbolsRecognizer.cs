#nullable enable
using System.Collections.Generic;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;

public static class ShaderDefineSymbolsRecognizer
{
    private static readonly Dictionary<string, IShaderDefineSymbolDescriptor> ourDescriptors = new();

    static ShaderDefineSymbolsRecognizer()
    {
        foreach (var descriptor in Descriptors)
        {
            foreach (var symbol in descriptor.AllSymbols)
                ourDescriptors.Add(symbol, descriptor);    
        }
    }

    public static IShaderDefineSymbolDescriptor? Recognize(string symbol) => ourDescriptors.TryGetValue(symbol, out var descriptor) ? descriptor : null;

    public static IShaderDefineSymbolDescriptor[] Descriptors => new IShaderDefineSymbolDescriptor[]
    {
        ShaderApiDefineSymbolDescriptor.Instance, ShaderPlatformDefineSymbolDescriptor.Instance,
        UrtCompilationModeDefineSymbolDescriptor.Instance
    };
}