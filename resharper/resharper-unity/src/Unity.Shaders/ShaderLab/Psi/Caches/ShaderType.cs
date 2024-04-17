#nullable enable
namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;

public enum ShaderType : byte
{
    Unknown,
    VertFrag,
    Surface,
    Compute
}