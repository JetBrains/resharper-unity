using System;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language;

[Flags]
public enum ShaderFeatureType
{
    None,
    KeywordList,
    KeywordListWithDisabledVariantForSingleKeyword,
    ImplicitKeywords,
    ImplicitKeywordsWithDisabledVariant
}