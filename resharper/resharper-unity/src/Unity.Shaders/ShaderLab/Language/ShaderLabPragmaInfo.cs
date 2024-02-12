using System.Collections.Immutable;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language;

public record ShaderLabPragmaInfo
{
    public ShaderFeatureType ShaderFeatureType { init; get; }
    public ImmutableArray<string> ImplicitKeywordSet { init; get; } = ImmutableArray<string>.Empty; 
    public int ImpliesShaderTarget { init; get; }

    public static ShaderLabPragmaInfo ForImplicitKeywordSet(params string[] keywords) => 
        new() { ShaderFeatureType = ShaderFeatureType.ImplicitKeywords, ImplicitKeywordSet = keywords.ToImmutableArray() };

    public static ShaderLabPragmaInfo ForImplicitKeywordSetWithDisabledVariant(params string[] keywords) =>
        new() { ShaderFeatureType = ShaderFeatureType.ImplicitKeywordsWithDisabledVariant, ImplicitKeywordSet = keywords.ToImmutableArray() };
}