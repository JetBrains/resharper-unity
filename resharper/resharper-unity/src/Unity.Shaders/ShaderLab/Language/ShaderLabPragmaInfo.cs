namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language;

public record ShaderLabPragmaInfo
{
    public bool DeclaresKeywords { init; get; }
    public bool HasDisabledVariantForSingleKeyword { init; get; }
    public int ImpliesShaderTarget { init; get; }
}