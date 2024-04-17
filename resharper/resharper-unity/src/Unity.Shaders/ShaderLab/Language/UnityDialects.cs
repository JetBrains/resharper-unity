#nullable enable
using JetBrains.Application;
using JetBrains.Application.Parts;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language;

[ShellComponent(Instantiation.DemandAnyThreadSafe)]
public sealed class UnityDialects
{
    public UnityHlslDialect HlslDialect { get; } = new();
    public UnityShaderLabHlslDialect ShaderLabHlslDialect { get; } = new();
    public UnityComputeHlslDialect ComputeHlslDialect { get; } = new();
}