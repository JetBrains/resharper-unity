#nullable enable
using System.Collections.Generic;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;

public static class ShaderDefineSymbolsRecognizer
{
    private static readonly Dictionary<string, IShaderDefineSymbolDescriptor> ourDescriptors = new();

    static ShaderDefineSymbolsRecognizer()
    {
        var shaderApiDescriptor = ShaderApiDefineSymbolDescriptor.Instance;
        foreach (var symbol in shaderApiDescriptor.AllSymbols)
            ourDescriptors.Add(symbol, shaderApiDescriptor);
    }

    public static IShaderDefineSymbolDescriptor? Recognize(string symbol) => ourDescriptors.TryGetValue(symbol, out var descriptor) ? descriptor : null;
}