using JetBrains.Application;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language;

[ShellComponent]
public sealed class UnityDialects
{
    public UnityHlslDialect HlslDialect { get; } = new();
    public UnityShaderLabHlslDialect ShaderLabHlslDialect { get; } = new();
    public UnityComputeHlslDialect ComputeHlslDialect { get; } = new();
}